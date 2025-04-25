using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace XiaoZhi.Unity.IOT
{
    public static class Utils
    {
        public static string TypeString<T>()
        {
            var typeName = typeof(T).Name;
            return typeName switch
            {
                nameof(Int32) => "number",
                _ => typeName.ToLower()
            };
        }
    }

    public interface IProperty<out T> : IProperty
    {
        T GetValue();
    }

    public interface IProperty
    {
        string Name { get; }
        string Description { get; }
        Type ValueType { get; }
        void GetDescriptorJson(JsonWriter writer);
        void GetStateJson(JsonWriter writer);
    }

    public interface IParameter<T> : IParameter
    {
        T Value { get; set; }
    }

    public interface IParameter
    {
        bool Required { get; }
        string Name { get; }
        string Description { get; }
        Type ValueType { get; }
        void GetDescriptorJson(JsonWriter writer);
    }

    public class Parameter<T> : IParameter<T>
    {
        public Parameter(string name, string description, bool required = true)
        {
            Name = name;
            Description = description;
            Required = required;
        }

        public string Name { get; }

        public string Description { get; }

        public bool Required { get; }

        public Type ValueType => typeof(T);

        public T Value { get; set; }

        public void GetDescriptorJson(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("description");
            writer.WriteValue(Description);
            writer.WritePropertyName("type");
            writer.WriteValue(Utils.TypeString<T>());
            writer.WriteEndObject();
        }
    }

    public class ParameterList : IEnumerable<IParameter>
    {
        private readonly List<IParameter> _parameters = new();

        public ParameterList()
        {
        }

        public ParameterList(IEnumerable<IParameter> parameters)
        {
            _parameters.AddRange(parameters);
        }

        public void AddParameter<T>(string name, string description, bool required = true)
        {
            _parameters.Add(new Parameter<T>(name, description, required));
        }

        public void AddParameter(IParameter parameter)
        {
            _parameters.Add(parameter);
        }

        public IParameter this[string name] =>
            _parameters.FirstOrDefault(p => p.Name == name) ??
            throw new ArgumentException($"Parameter not found: {name}");

        public T GetValue<T>(string name)
        {
            if (this[name] is not IParameter<T> parameter)
            {
                Debug.LogError($"Cannot cast parameter value to type {typeof(T).Name}");
                return default;
            }

            return parameter.Value;
        }

        public void SetValue<T>(string name, T value)
        {
            if (this[name] is not IParameter<T> parameter)
            {
                Debug.LogError($"Cannot cast parameter value to type {typeof(T).Name}");
                return;
            }

            parameter.Value = value;
        }

        public void GetDescriptorJson(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var parameter in _parameters)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(parameter.Name);
                parameter.GetDescriptorJson(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public IEnumerator<IParameter> GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class Property<T> : IProperty<T>
    {
        private readonly Func<T> _getter;

        public Property(string name, string description, Func<T> getter)
        {
            Name = name;
            Description = description;
            _getter = getter;
        }

        public string Name { get; }

        public string Description { get; }

        public T Value => _getter();

        public Type ValueType => typeof(T);

        public T GetValue() => Value;

        public void GetDescriptorJson(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("description");
            writer.WriteValue(Description);
            writer.WritePropertyName("type");
            writer.WriteValue(Utils.TypeString<T>());
            writer.WriteEndObject();
        }

        public void GetStateJson(JsonWriter writer)
        {
            writer.WriteValue(Value);
        }
    }

    public class PropertyList
    {
        private readonly List<IProperty> _properties = new();

        public PropertyList()
        {
        }

        public PropertyList(IEnumerable<IProperty> properties)
        {
            _properties.AddRange(properties);
        }

        public void AddProperty<T>(string name, string description, Func<T> getter)
        {
            _properties.Add(new Property<T>(name, description, getter));
        }

        public void AddProperty(IProperty property)
        {
            _properties.Add(property);
        }

        public IProperty this[string name] =>
            _properties.FirstOrDefault(p => p.Name == name) ??
            throw new ArgumentException($"Property not found: {name}");

        public T GetValue<T>(string name)
        {
            if (this[name] is not IProperty<T> property)
            {
                Debug.LogError($"Cannot cast property value to type {typeof(T).Name}");
                return default;
            }

            return property.GetValue();
        }

        public void GetDescriptorJson(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var property in _properties)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(property.Name);
                property.GetDescriptorJson(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public void GetStateJson(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var property in _properties)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(property.Name);
                property.GetStateJson(writer);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }

    public class Method
    {
        private readonly string _name;
        private readonly string _description;
        private readonly ParameterList _parameters;
        private readonly Action<ParameterList> _callback;

        public Method(string name, string description, ParameterList parameters, Action<ParameterList> callback)
        {
            _name = name;
            _description = description;
            _parameters = parameters;
            _callback = callback;
        }

        public string Name => _name;
        public string Description => _description;
        public ParameterList Parameters => _parameters;

        public void GetDescriptorJson(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("description");
            writer.WriteValue(_description);
            writer.WritePropertyName("parameters");
            _parameters.GetDescriptorJson(writer);
            writer.WriteEndObject();
        }

        public void Invoke()
        {
            _callback(_parameters);
        }
    }

    public class MethodList
    {
        private readonly List<Method> _methods = new();

        public MethodList()
        {
        }

        public MethodList(IEnumerable<Method> methods)
        {
            _methods.AddRange(methods);
        }

        public void AddMethod(string name, string description, ParameterList parameters, Action<ParameterList> callback)
        {
            _methods.Add(new Method(name, description, parameters, callback));
        }

        public Method this[string name] =>
            _methods.FirstOrDefault(m => m.Name == name) ??
            throw new ArgumentException($"Method not found: {name}");

        public void GetDescriptorJson(JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var method in _methods)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(method.Name);
                method.GetDescriptorJson(writer);
                writer.WriteEndObject();
            }
        }
    }

    public abstract class Thing
    {
        protected readonly PropertyList Properties;
        protected readonly MethodList Methods;
        protected Context Context;
        
        public string Name { get; }

        public string Description { get; }

        protected Thing(string name, string description)
        {
            Name = name;
            Description = description;
            Properties = new PropertyList();
            Methods = new MethodList();
        }

        public void Inject(Context ctx)
        {
            Context = ctx;
        }

        public void GetDescriptorJson(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("name");
            writer.WriteValue(Name);
            writer.WritePropertyName("description");
            writer.WriteValue(Description);
            writer.WritePropertyName("properties");
            Properties.GetDescriptorJson(writer);
            writer.WritePropertyName("methods");
            Methods.GetDescriptorJson(writer);
            writer.WriteEndObject();
        }

        public string GetStateJson()
        {
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("name");
            jsonWriter.WriteValue(Name);
            jsonWriter.WritePropertyName("state");
            Properties.GetStateJson(jsonWriter);
            jsonWriter.WriteEndObject();
            return stringWriter.ToString();
        }

        public void Invoke(JToken command)
        {
            try
            {
                var methodName = command["method"].Value<string>();
                var inputParams = command["parameters"];
                var method = Methods[methodName];
                foreach (var param in method.Parameters)
                {
                    var inputParam = inputParams?[param.Name];
                    if (param.Required && inputParam == null)
                        throw new ArgumentException($"Parameter {param.Name} is required");
                    if (inputParam == null) continue;
                    var genericType = param.ValueType;
                    var commandValue = typeof(Extensions).GetMethod("Value", BindingFlags.Public | BindingFlags.Static, null,
                        new[] { typeof(IEnumerable<JToken>) }, null)!.MakeGenericMethod(genericType);
                    var value = commandValue.Invoke(null, new object[] { inputParam });
                    var paramValue = param.GetType().GetProperty("Value");
                    paramValue!.SetValue(param, value);
                }

                method.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Method invocation failed: {ex}");
            }
        }
    }
}