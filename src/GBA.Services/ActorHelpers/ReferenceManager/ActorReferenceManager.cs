using System;
using System.Collections.Generic;
using Akka.Actor;

namespace GBA.Services.ActorHelpers.ReferenceManager;

public sealed class ActorReferenceManager : Singleton<ActorReferenceManager> {
    private readonly Dictionary<string, IActorRef> _container = new();

    private readonly object _lock = new();

    public void Add(string name, IActorRef actorRef) {
        lock (_lock) {
            if (_container.ContainsKey(name))
                throw new Exception("Actor Reference with specified name already exists");

            _container.Add(name, actorRef);
        }
    }

    public IActorRef Get(string name) {
        lock (_lock) {
            if (!_container.ContainsKey(name))
                throw new Exception("Manager does not contains reference to specified actor name");

            return _container[name];
        }
    }
}