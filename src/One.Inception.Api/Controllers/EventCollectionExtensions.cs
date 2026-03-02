using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using One.Inception.Discoveries;

namespace One.Inception.Api.Controllers;

public static class EventCollectionExtensions
{
    private static ICollection<Event_Response_New> eventsResponse;

    public static ICollection<Event_Response_New> GetAndCacheEvents()
    {
        if (eventsResponse is not null)
            return eventsResponse;

        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.IsDynamic == false);

        ICollection<Event_Response_New> allEvents = GetEvents(loadedAssemblies);

        eventsResponse = allEvents;
        return eventsResponse;
    }

    private static ICollection<Event_Response_New> GetEvents(IEnumerable<Assembly> loadedAssemblies)
    {
        var events = RetrieveTypesFromAssemblies(loadedAssemblies,
           x => typeof(IEvent).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0,
           meta => new Event_Response_New
           {
               Id = meta.GetCustomAttribute<DataContractAttribute>().Name,
               Name = meta.Name,
               BC = meta.GetCustomAttribute<DataContractAttribute>().Namespace,
               Properties = meta.GetProperties(BindingFlags.Public).Select(x => x.Name).ToList(),
               IsPublicEvent = false
           });

        var publicEvents = RetrieveTypesFromAssemblies(loadedAssemblies,
           x => typeof(IPublicEvent).IsAssignableFrom(x) && x.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0,
           meta => new Event_Response_New
           {
               Id = meta.GetCustomAttribute<DataContractAttribute>().Name,
               Name = meta.Name,
               BC = meta.GetCustomAttribute<DataContractAttribute>().Namespace,
               Properties = meta.GetProperties(BindingFlags.Public).Select(x => x.Name).ToList(),
               IsPublicEvent = true
           });
        var allEvents = events.Concat(publicEvents);
        return allEvents.ToList();
    }

    private static ICollection<TResult> RetrieveTypesFromAssemblies<TResult>(IEnumerable<Assembly> loadedAssemblies, Func<Type, bool> typeFilter, Func<Type, TResult> retrieveResult)
    {
        var result = new List<TResult>();

        var typesMeta = loadedAssemblies
            .SelectMany(ass => ass.GetLoadableTypes()
            .Where(typeFilter));

        foreach (var typeMeta in typesMeta)
        {
            result.Add(retrieveResult(typeMeta));
        }

        return result;
    }
}

public class Event_Response_New
{
    public Event_Response_New()
    {
        Properties = new List<string>();
    }

    public string Id { get; set; }

    public string Name { get; set; }

    public string BC { get; set; }

    public bool IsPublicEvent { get; set; }

    public List<string> Properties { get; set; }
}
