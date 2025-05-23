﻿using fiskaltrust.Middleware.Localization.v2.Configuration;
using Newtonsoft.Json;

namespace fiskaltrust.Middleware.Localization.QueueGR;

public class QueueGRConfiguration
{
    public bool Sandbox { get; set; } = false;

    public static QueueGRConfiguration FromMiddlewareConfiguration(MiddlewareConfiguration middlewareConfiguration) => JsonConvert.DeserializeObject<QueueGRConfiguration>(JsonConvert.SerializeObject(middlewareConfiguration.Configuration));
}