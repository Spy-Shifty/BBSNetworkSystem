using System;
using System.Runtime.Serialization;

[Serializable]
internal class NetworkEntityFactoryMethodNotFoundException : Exception {
   

    public NetworkEntityFactoryMethodNotFoundException() {
    }

    public NetworkEntityFactoryMethodNotFoundException(int id) : base (string.Format("Could not found a NetworkEntityFactoryMethod with id: {0}", id) ){

    }

    public NetworkEntityFactoryMethodNotFoundException(string message) : base(message) {
    }

    public NetworkEntityFactoryMethodNotFoundException(string message, Exception innerException) : base(message, innerException) {
    }

    protected NetworkEntityFactoryMethodNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
    }
}