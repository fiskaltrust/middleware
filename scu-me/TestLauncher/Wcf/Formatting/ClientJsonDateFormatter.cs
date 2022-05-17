#if NET461
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace fiskaltrust.Middleware.SCU.ME.Test.Launcher.Wcf.Formatting
{
    public class ClientJsonDateFormatter : IOperationBehavior
    {
        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters) { }

        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation) { }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation) => dispatchOperation.Formatter = new JsonDispatchMessageFormatter(operationDescription, dispatchOperation.Formatter);

        public void Validate(OperationDescription operationDescription) { }
    }
}
#endif