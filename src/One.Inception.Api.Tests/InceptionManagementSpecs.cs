using Machine.Specifications;
using Microsoft.AspNetCore.Mvc;
using One.Inception;
using One.Inception.Api.Controllers;
using One.Inception.EventStore.Index;
using One.Inception.EventStore.Index.Handlers;
using One.Inception.EventStore.Players;
using One.Inception.Projections;
using System;
using System.Linq;

namespace One.Inception.Api.Tests;

[Subject("InceptionManagement")]
public class When_ping_is_called
{
    Establish context = () =>
    {
        management = FacadeFactory.Build().management;
    };

    Because of = () =>
    {
        result = management.Ping();
    };

    It should_respond_with_pong = () =>
    {
        (result as OkObjectResult).Value.ShouldEqual("pong");
    };

    static InceptionManagement management;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_get_tenants_is_called
{
    Establish context = () =>
    {
        management = FacadeFactory.Build().management;
    };

    Because of = () =>
    {
        result = management.GetTenants();
    };

    It should_return_the_configured_tenants = () =>
    {
        var value = (result as OkObjectResult).Value as string[];
        value.ShouldNotBeNull();
        value.ShouldContain("tenant1");
        value.ShouldContain("tenant2");
    };

    static InceptionManagement management;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_wiping_the_current_tenant
{
    Establish context = () =>
    {
        (management, _, _, _, _, _, _, _, dangerZone) = FacadeFactory.Build();
    };

    Because of = async () =>
    {
        result = await management.WipeAsync("tenant1");
    };

    It should_wipe_the_registered_danger_zones = () =>
    {
        dangerZone.Wiped.ShouldContain("tenant1");
    };

    It should_respond_ok = () =>
    {
        result.ShouldNotBeNull();
        (result as OkObjectResult).Value.ShouldNotBeNull();
    };

    static InceptionManagement management;
    static RecordingDangerZone dangerZone;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_wiping_a_different_tenant
{
    Establish context = () =>
    {
        (management, _, _, _, _, _, _, _, dangerZone) = FacadeFactory.Build();
    };

    Because of = async () =>
    {
        result = await management.WipeAsync("other-tenant");
    };

    It should_not_wipe_any_danger_zone = () =>
    {
        dangerZone.Wiped.ShouldBeEmpty();
    };

    It should_still_respond_ok = () =>
    {
        (result as OkObjectResult).Value.ShouldNotBeNull();
    };

    static InceptionManagement management;
    static RecordingDangerZone dangerZone;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_replaying_a_public_event_to_the_same_bounded_context
{
    Establish context = () =>
    {
        (management, _, _, _, _, _, signalPublisher, _, _) = FacadeFactory.Build();
        model = new ReplayEventRequest
        {
            Tenant = "tenant1",
            RecipientBoundedContext = "One.Inception.Api.Tests",
            RecipientHandlers = "some-handler",
            SourceEventTypeId = "bgib29-test-public-event",
        };
    };

    Because of = async () =>
    {
        result = await management.ReplayEvent(model);
    };

    It should_reject_the_replay = () =>
    {
        ((ResponseResult<string>)(result as BadRequestObjectResult).Value).Result.ShouldContain("Can not publish public event in the same bounded context");
    };

    It should_not_publish_anything = () =>
    {
        signalPublisher.Published.ShouldBeEmpty();
    };

    static InceptionManagement management;
    static RecordingPublisher<ISystemSignal> signalPublisher;
    static ReplayEventRequest model;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_replaying_an_internal_event_to_the_same_bounded_context
{
    Establish context = () =>
    {
        (management, _, _, _, _, _, signalPublisher, _, _) = FacadeFactory.Build();
        model = new ReplayEventRequest
        {
            Tenant = "tenant1",
            RecipientBoundedContext = "One.Inception.Api.Tests",
            RecipientHandlers = "some-handler",
            SourceEventTypeId = "bgib29-test-event",
        };
    };

    Because of = async () =>
    {
        result = await management.ReplayEvent(model);
    };

    It should_respond_ok = () =>
    {
        (result as OkObjectResult).Value.ShouldNotBeNull();
    };

    It should_publish_the_internal_replay_signal = () =>
    {
        signalPublisher.Published.Count.ShouldEqual(1);
        var signal = (ReplayInternalEventsRequested)signalPublisher.Published.Single().Message;
        signal.Tenant.ShouldEqual("tenant1");
        signal.RecipientBoundedContext.ShouldEqual("One.Inception.Api.Tests");
        signal.SourceEventTypeId.ShouldEqual("bgib29-test-event");
    };

    static InceptionManagement management;
    static RecordingPublisher<ISystemSignal> signalPublisher;
    static ReplayEventRequest model;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_replaying_a_public_event_to_another_bounded_context
{
    Establish context = () =>
    {
        (management, _, _, _, _, _, signalPublisher, _, _) = FacadeFactory.Build();
        model = new ReplayEventRequest
        {
            Tenant = "tenant1",
            RecipientBoundedContext = "other-bc",
            RecipientHandlers = "some-handler",
            SourceEventTypeId = "bgib29-test-public-event",
        };
    };

    Because of = async () =>
    {
        result = await management.ReplayEvent(model);
    };

    It should_respond_ok = () =>
    {
        (result as OkObjectResult).Value.ShouldNotBeNull();
    };

    It should_publish_the_public_replay_signal = () =>
    {
        signalPublisher.Published.Count.ShouldEqual(1);
        signalPublisher.Published.Single().Message.ShouldBeAssignableTo<ReplayPublicEventsRequested>();
    };

    static InceptionManagement management;
    static RecordingPublisher<ISystemSignal> signalPublisher;
    static ReplayEventRequest model;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_replaying_an_unknown_event
{
    Establish context = () =>
    {
        (management, _, _, _, _, _, signalPublisher, _, _) = FacadeFactory.Build();
        model = new ReplayEventRequest
        {
            Tenant = "tenant1",
            RecipientBoundedContext = "other-bc",
            RecipientHandlers = "some-handler",
            SourceEventTypeId = "no-such-event",
        };
    };

    Because of = async () =>
    {
        result = await management.ReplayEvent(model);
    };

    It should_reject_the_replay = () =>
    {
        ((ResponseResult<string>)(result as BadRequestObjectResult).Value).Result.ShouldContain("Unable to start replay");
    };

    It should_not_publish_anything = () =>
    {
        signalPublisher.Published.ShouldBeEmpty();
    };

    static InceptionManagement management;
    static RecordingPublisher<ISystemSignal> signalPublisher;
    static ReplayEventRequest model;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_list_commands_is_called
{
    Establish context = () =>
    {
        management = FacadeFactory.Build().management;
    };

    Because of = () =>
    {
        result = management.ListCommands();
    };

    It should_include_the_discovered_commands = () =>
    {
        var dto = (result as OkObjectResult).Value as ResponseResult<CommandListDto>;
        dto.Result.Commands.Select(x => x.DataContract).ShouldContain("bgib29-test-command");
    };

    static InceptionManagement management;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_exploring_the_domain
{
    Establish context = () =>
    {
        management = FacadeFactory.Build().management;
    };

    Because of = () =>
    {
        result = management.ExploreDomain();
    };

    It should_list_the_discovered_aggregate = () =>
    {
        var dto = (result as OkObjectResult).Value as Domain_Response;
        dto.Aggregates.ShouldContain(x => x.Name == "Bgib29TestRoot");
    };

    It should_list_the_when_events_of_the_aggregate = () =>
    {
        var dto = (result as OkObjectResult).Value as Domain_Response;
        var aggregate = dto.Aggregates.Single(x => x.Name == "Bgib29TestRoot");
        aggregate.Events.Select(x => x.Id).ShouldContain("bgib29-test-event");
    };

    It should_list_the_discovered_events = () =>
    {
        var dto = (result as OkObjectResult).Value as Domain_Response;
        dto.Events.Select(x => x.Id).ShouldContain("bgib29-test-event");
        dto.Events.Select(x => x.Id).ShouldContain("bgib29-test-public-event");
    };

    It should_list_the_discovered_commands = () =>
    {
        var dto = (result as OkObjectResult).Value as Domain_Response;
        dto.Commands.Select(x => x.Id).ShouldContain("bgib29-test-command");
    };

    static InceptionManagement management;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_getting_aggregate_id_samples
{
    Establish context = () =>
    {
        management = FacadeFactory.Build().management;
    };

    Because of = () =>
    {
        result = management.GetAggregateRootIdSamples("tenant1");
    };

    It should_produce_a_sample_for_the_custom_generic_id = () =>
    {
        var dto = (result as OkObjectResult).Value as Domain_Response;
        dto.AggregateIdSamples.ShouldContain(x => x.IdSample == "urn:tenant1:bgib29testid:id");
    };

    static InceptionManagement management;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_listing_indices_with_no_indices
{
    Establish context = () =>
    {
        management = FacadeFactory.Build().management;
    };

    Because of = async () =>
    {
        result = await management.ListIndices();
    };

    It should_respond_ok_with_an_empty_list = () =>
    {
        var dto = (result as OkObjectResult).Value as ResponseResult<IndexListDto>;
        dto.Result.Indices.ShouldBeEmpty();
    };

    static InceptionManagement management;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_getting_event_store_index_meta
{
    Establish context = () =>
    {
        var indices = new TypeContainer<IEventStoreIndex>(new Type[] { typeof(Bgib29TestIndex) });
        var fakes = FacadeFactory.Build(indices);
        management = fakes.management;
        fakes.projectionReader.Result = new ReadResult<IProjectionDefinition>(EventStoreIndexStatusHelper.WithStatus(IndexStatus.Present));
    };

    Because of = async () =>
    {
        result = await management.GetEventStoreIndexMeta();
    };

    It should_return_the_index_status = () =>
    {
        var values = (result as OkObjectResult).Value as ResponseResult<System.Collections.Generic.List<MetaResponseModel>>;
        values.Result.Count.ShouldEqual(1);
        values.Result.Single().Id.ShouldEqual("bgib29-test-index");
        values.Result.Single().Status.ShouldEqual(typeof(EventStoreIndexStatusState).FullName);
    };

    static InceptionManagement management;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_rebuilding_an_event_store_index
{
    Establish context = () =>
    {
        var fakes = FacadeFactory.Build();
        management = fakes.management;
        commandPublisher = fakes.commandPublisher;
        model = new IndexRequestModel { IndexContractId = "bgib29-test-index", MaxDegreeOfParallelism = 4 };
    };

    Because of = async () =>
    {
        result = await management.RebuildIndex(model);
    };

    It should_publish_the_rebuild_command = () =>
    {
        commandPublisher.Published.Count.ShouldEqual(1);
    };

    It should_respond_ok_when_published = () =>
    {
        (result as OkObjectResult).Value.ShouldNotBeNull();
    };

    static InceptionManagement management;
    static RecordingPublisher<ICommand> commandPublisher;
    static IndexRequestModel model;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_rebuilding_an_event_store_index_fails_to_publish
{
    Establish context = () =>
    {
        var fakes = FacadeFactory.Build();
        management = fakes.management;
        fakes.commandPublisher.Success = false;
        model = new IndexRequestModel { IndexContractId = "bgib29-test-index", MaxDegreeOfParallelism = 4 };
    };

    Because of = async () =>
    {
        result = await management.RebuildIndex(model);
    };

    It should_respond_bad_request = () =>
    {
        ((ResponseResult<string>)(result as BadRequestObjectResult).Value).Result.ShouldContain("Unable to publish command");
    };

    static InceptionManagement management;
    static IndexRequestModel model;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_getting_meta_of_an_unknown_projection
{
    Establish context = () =>
    {
        management = FacadeFactory.Build().management;
        model = new ProjectionMetaRequestModel { ProjectionContractId = "no-such-projection" };
    };

    Because of = async () =>
    {
        result = await management.GetProjectionMeta(model);
    };

    It should_respond_bad_request = () =>
    {
        ((ResponseResult<string>)(result as BadRequestObjectResult).Value).Result.ShouldContain("not found");
    };

    static InceptionManagement management;
    static ProjectionMetaRequestModel model;
    static IActionResult result;
}

[Subject("InceptionManagement")]
public class When_the_facade_is_created_with_a_null_dependency
{
    Because of = () =>
    {
        try
        {
            new InceptionManagement(
                null, null, null, null, null, null, null, null, null, null, null, null, null);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
    };

    It should_throw_an_argument_null_exception = () =>
    {
        exception.ShouldBeAssignableTo<ArgumentNullException>();
    };

    static Exception exception;
}
