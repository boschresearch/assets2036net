# assets2036net

## Description

The assets2036net library and package supports .NET developers in participating in the 
assets2036 MQTT-based communication. It depends on M2MqttDotNetCore for MQTT communication. 

## Getting started

For a very basic usage see take a look into the CLI project SimplePropertyAndOperation. 
Herein an asset is created and equipped with some properties and operations. Then an asset 
proxy is created which reads the properties and calls the asset's operation. 

The used submodel descriptions are 
* [six-axis-robot](https://raw.githubusercontent.com/boschresearch/assets2036-submodels/master/six-axis-robot.json)
* [light](https://raw.githubusercontent.com/boschresearch/assets2036-submodels/master/light.json)

Those submodels are very simple / basic and offer only simple datatypes in properties and 
operation parameters. In the unit tests project you will find a sufficient set of all imaginable 
use cases, parameter types, return types. 

## Remarks

The focus in assets2036 is simplicity and leanness - this also aplies to the librares. This 
is why in some places one might miss a more clean separation of concerns in the implementation. 

* There is only one set of classes for Submodel and the submodel elements (Properties, Events, 
Operations) in the library. They are used for the serialization and deserialization of the JSON 
submodel descriptions (JSON model files read from submodel repository) as well as for the access 
to the submodel elements at runtime. This might lead to some confusion and sometimes made it 
impossible to strictly hide unneccessary aspects of the API from the developer. 

## Dependencies (NuGet)

### assets2036net library

- log4net	2.0.13	Apache-2.0
- MQTTNet	3.1.1	MIT 

### assets2036 unittests
- Microsoft.NET.Test.Sdk	17.0.0	MIT 
- xunit	2.4.1	MIT 
- xunit.runner.visualstudio	2.4.3	MIT 
- log4net	2.0.13	Apache-2.0


## Authors

[Thomas Jung](https://github.com/thomasjosefjung)

## License 

Apache-2.0 License

## Acknowledgments

Thanks to [Daniel Ewert](https://github.com/DaEwe/) for the inspiration, conceptual work and 
preliminary [python library](https://github.com/boschresearch/assets2036py). 
