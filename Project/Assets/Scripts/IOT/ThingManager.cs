using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace XiaoZhi.Unity.IOT
{
    public class ThingManager
    {
        private Context _context;
        private readonly List<Thing> _things = new();
        private readonly Dictionary<string, string> _lastStates = new();

        public void Inject(Context ctx)
        {
            _context = ctx;
        }

        public void AddThing(Thing thing)
        {
            thing.Inject(_context);
            _things.Add(thing);
        }

        public string GetDescriptorsJson()
        {
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            jsonWriter.WriteStartArray();
            foreach (var thing in _things)
                thing.GetDescriptorJson(jsonWriter);
            jsonWriter.WriteEndArray();
            return stringWriter.ToString();
        }

        public bool GetStatesJson(out string json, bool delta = false)
        {
            if (!delta)
            {
                _lastStates.Clear();
            }

            var changed = false;
            var states = new List<string>();

            foreach (var thing in _things)
            {
                var state = thing.GetStateJson();
                if (delta)
                {
                    if (_lastStates.TryGetValue(thing.Name, out var lastState) && lastState == state) continue;
                    changed = true;
                    _lastStates[thing.Name] = state;
                }

                states.Add(state);
            }

            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            jsonWriter.WriteStartArray();
            foreach (var state in states)
                jsonWriter.WriteRawValue(state);
            jsonWriter.WriteEndArray();
            json = stringWriter.ToString();
            return changed;
        }

        public void Invoke(JToken command)
        {
            var name = command["name"]?.Value<string>();
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("Command must contain a 'name' property");
                return;
            }

            var thing = _things.FirstOrDefault(t => t.Name == name);
            if (thing == null)
            {
                Debug.LogError($"Thing not found: {name}");
                return;
            }

            thing.Invoke(command);
        }
    }
}