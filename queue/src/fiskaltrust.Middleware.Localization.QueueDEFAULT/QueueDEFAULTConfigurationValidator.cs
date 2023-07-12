using System;

namespace fiskaltrust.Middleware.Localization.QueueDEFAULT
{
    public class QueueDEFAULTConfigurationValidator
    {
        private readonly QueueDEFAULTConfiguration _config;

        public QueueDEFAULTConfigurationValidator(QueueDEFAULTConfiguration config)
        {
            _config = config;
        }

        public void Validate()
        {
            if (!_config.Sandbox)
            {
                throw new InvalidOperationException("Only sandbox mode is allowed in this context.");
            }
        }
    }
}