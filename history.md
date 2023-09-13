# Compass.NET Version History

- 1.0.0-preview1: Initial release
- 1.0.0-preview2:
  - *introduced*: `RouterBuilder.AddRoute()` overload
  - *done*: Some code refinements
- 1.0.0-preview3:
  - *introduced*: `IConverter` interface (replacing the `TryConvert` delegate)
  - *introduced*: `RouteTemplate.CreateCompiler()` method
- 1.0.0-preview4:
  - *introduced*: `ISplitOptions` enum
  - *introduced*: `netstandard2.0` support
- 1.0.0-preview5:
  - *breaking*: New `SplitOptions` layout
  - *fixed*: Missing URL encoding when calling the `RouteTemplateCompiler` delegate 
  - *done*: Rewritten percent-decoder