// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Builder;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Controller;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Queue;
using KubeOps.Operator.Reconciliation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using ZiggyCreatures.Caching.Fusion;

namespace KubeOps.Operator.Test.Reconciliation;

/// <summary>
/// Tests for the multi-controller dispatch logic in <see cref="Reconciler{TEntity}"/>.
/// Verifies that <see cref="IEntityController{TEntity}.LabelFilter"/> is respected when
/// multiple controllers are registered for the same entity type.
/// </summary>
public sealed class ReconcilerMultiControllerTest
{
    private readonly Mock<ILogger<Reconciler<V1ConfigMap>>> _mockLogger = new();
    private readonly Mock<IFusionCacheProvider> _mockCacheProvider = new();
    private readonly Mock<IFusionCache> _mockCache = new();
    private readonly Mock<IKeyedServiceProvider> _mockServiceProvider = new();
    private readonly Mock<IKubernetesClient> _mockClient = new();
    private readonly Mock<ITimedEntityQueue<V1ConfigMap>> _mockQueue = new();
    private readonly OperatorSettings _settings = new() { AutoAttachFinalizers = false, AutoDetachFinalizers = false };

    public ReconcilerMultiControllerTest()
    {
        _mockCacheProvider
            .Setup(p => p.GetCache(It.IsAny<string>()))
            .Returns(_mockCache.Object);
    }

    // ── null LabelFilter = catch-all ──────────────────────────────────────────

    [Fact]
    public async Task Dispatch_NullLabelFilter_MatchesEntityWithNoLabels()
    {
        var entity = CreateEntity();
        var controller = CreateController(labelFilter: null);
        var reconciler = CreateReconciler([controller.Object]);

        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Added);
        await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        controller.Verify(c => c.ReconcileAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_NullLabelFilter_MatchesEntityWithLabels()
    {
        var entity = CreateEntity(labels: new() { ["env"] = "prod" });
        var controller = CreateController(labelFilter: null);
        var reconciler = CreateReconciler([controller.Object]);

        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Added);
        await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        controller.Verify(c => c.ReconcileAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── label filter matching ─────────────────────────────────────────────────

    [Fact]
    public async Task Dispatch_LabelFilterMatches_ControllerIsCalled()
    {
        var entity = CreateEntity(labels: new() { ["env"] = "prod" });
        var controller = CreateController(labelFilter: "env in (prod)");
        var reconciler = CreateReconciler([controller.Object]);

        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Added);
        await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        controller.Verify(c => c.ReconcileAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_LabelFilterDoesNotMatch_ControllerIsNotCalled()
    {
        var entity = CreateEntity(labels: new() { ["env"] = "staging" });
        var controller = CreateController(labelFilter: "env in (prod)");
        var reconciler = CreateReconciler([controller.Object]);

        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Added);
        await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        controller.Verify(c => c.ReconcileAsync(It.IsAny<V1ConfigMap>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_EntityHasNoLabels_FilteredControllerIsNotCalled()
    {
        var entity = CreateEntity();
        var controller = CreateController(labelFilter: "env in (prod)");
        var reconciler = CreateReconciler([controller.Object]);

        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Added);
        await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        controller.Verify(c => c.ReconcileAsync(It.IsAny<V1ConfigMap>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── multiple controllers ──────────────────────────────────────────────────

    [Fact]
    public async Task Dispatch_TwoMatchingControllers_BothCalledInOrder()
    {
        var entity = CreateEntity(labels: new() { ["env"] = "prod" });
        var callOrder = new List<int>();

        var ctrl1 = CreateController(labelFilter: "env in (prod)", onReconcile: e =>
        {
            callOrder.Add(1);
            return ReconciliationResult<V1ConfigMap>.Success(e);
        });
        var ctrl2 = CreateController(labelFilter: null, onReconcile: e =>
        {
            callOrder.Add(2);
            return ReconciliationResult<V1ConfigMap>.Success(e);
        });

        var reconciler = CreateReconciler([ctrl1.Object, ctrl2.Object]);
        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Added);
        await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        callOrder.Should().Equal(1, 2);
    }

    [Fact]
    public async Task Dispatch_TwoControllers_OnlyMatchingOneIsCalled()
    {
        var entity = CreateEntity(labels: new() { ["env"] = "prod" });
        var prodCtrl = CreateController(labelFilter: "env in (prod)");
        var stagingCtrl = CreateController(labelFilter: "env in (staging)");

        var reconciler = CreateReconciler([prodCtrl.Object, stagingCtrl.Object]);
        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Added);
        await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        prodCtrl.Verify(c => c.ReconcileAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        stagingCtrl.Verify(c => c.ReconcileAsync(It.IsAny<V1ConfigMap>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_FirstControllerFails_SecondControllerNotCalled()
    {
        var entity = CreateEntity(labels: new() { ["env"] = "prod" });
        var failResult = ReconciliationResult<V1ConfigMap>.Failure(entity, "first failed");

        var ctrl1 = CreateController(labelFilter: null, onReconcile: _ => failResult);
        var ctrl2 = CreateController(labelFilter: null);

        var reconciler = CreateReconciler([ctrl1.Object, ctrl2.Object]);
        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Added);
        var result = await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("first failed");
        ctrl2.Verify(c => c.ReconcileAsync(It.IsAny<V1ConfigMap>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Dispatch_NoControllerMatches_ReturnsSuccess()
    {
        var entity = CreateEntity(labels: new() { ["env"] = "prod" });
        var controller = CreateController(labelFilter: "env in (staging)");

        var reconciler = CreateReconciler([controller.Object]);
        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Added);
        var result = await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Dispatch_NoControllerMatches_LogsWarning()
    {
        var entity = CreateEntity(labels: new() { ["env"] = "prod" });
        var controller = CreateController(labelFilter: "env in (staging)");

        var reconciler = CreateReconciler([controller.Object]);
        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Added);
        await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains(entity.Name())),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── entity is passed through the chain ────────────────────────────────────

    [Fact]
    public async Task Dispatch_ControllerMutatesEntity_NextControllerReceivesMutatedEntity()
    {
        var original = CreateEntity();
        var mutated = CreateEntity(name: "mutated-configmap");

        var ctrl1 = CreateController(labelFilter: null, onReconcile: _ =>
            ReconciliationResult<V1ConfigMap>.Success(mutated));

        V1ConfigMap? receivedByCtrl2 = null;
        var ctrl2 = CreateController(labelFilter: null, onReconcile: e =>
        {
            receivedByCtrl2 = e;
            return ReconciliationResult<V1ConfigMap>.Success(e);
        });

        var reconciler = CreateReconciler([ctrl1.Object, ctrl2.Object]);
        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(original, WatchEventType.Added);
        await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        receivedByCtrl2.Should().BeSameAs(mutated);
    }

    // ── DeletedAsync path ────────────────────────────────────────────────────

    [Fact]
    public async Task Dispatch_DeletedEvent_MatchingControllerDeletedAsyncCalled()
    {
        var entity = CreateEntity(labels: new() { ["env"] = "prod" });
        var controller = CreateController(labelFilter: "env in (prod)");

        var reconciler = CreateReconciler([controller.Object]);
        var context = ReconciliationContext<V1ConfigMap>.CreateFromApiServerEvent(entity, WatchEventType.Deleted);
        await reconciler.Reconcile(context, TestContext.Current.CancellationToken);

        controller.Verify(c => c.DeletedAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        controller.Verify(c => c.ReconcileAsync(It.IsAny<V1ConfigMap>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Reconciler<V1ConfigMap> CreateReconciler(IList<IEntityController<V1ConfigMap>> controllers)
    {
        var mockScope = new Mock<IServiceScope>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();

        mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        mockScopeFactory.Setup(s => s.CreateScope()).Returns(mockScope.Object);

        _mockServiceProvider
            .Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);

        _mockServiceProvider
            .Setup(p => p.GetService(typeof(IEnumerable<IEntityController<V1ConfigMap>>)))
            .Returns(controllers);

        return new(
            _mockLogger.Object,
            _mockCacheProvider.Object,
            _mockServiceProvider.Object,
            _settings,
            _mockQueue.Object,
            _mockClient.Object);
    }

    private static Mock<IEntityController<V1ConfigMap>> CreateController(
        string? labelFilter,
        Func<V1ConfigMap, ReconciliationResult<V1ConfigMap>>? onReconcile = null)
    {
        var mock = new Mock<IEntityController<V1ConfigMap>>();

        mock.Setup(c => c.LabelFilter).Returns(labelFilter);

        mock.Setup(c => c.ReconcileAsync(It.IsAny<V1ConfigMap>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1ConfigMap e, CancellationToken _) =>
                onReconcile?.Invoke(e) ?? ReconciliationResult<V1ConfigMap>.Success(e));

        mock.Setup(c => c.DeletedAsync(It.IsAny<V1ConfigMap>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((V1ConfigMap e, CancellationToken _) =>
                ReconciliationResult<V1ConfigMap>.Success(e));

        return mock;
    }

    private static V1ConfigMap CreateEntity(
        string? name = null,
        Dictionary<string, string>? labels = null) =>
        new()
        {
            Metadata = new()
            {
                Name = name ?? "test-configmap",
                NamespaceProperty = "default",
                Uid = Guid.NewGuid().ToString(),
                Generation = 1,
                Labels = labels,
            },
            Kind = V1ConfigMap.KubeKind,
        };
}
