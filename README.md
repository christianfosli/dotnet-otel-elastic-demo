# .NET Open Telemetry Demo

Code base for blog article I'm writing about OpenTelemetry tracing for
dotnet services and visualizing the data with Elastic/Kibana/APM.

Read the [blog post here](https://medium.com/capgemini-norway/visualisere-telemetridata-fra-net-mikrotjenester-med-opentelemetry-elasticsearch-kibana-c4121c8b68e4)
(in Norwegian).

### Running locally with Docker Desktop

```
kubectl config use-context docker-desktop
./scripts/build-and-run.sh
```

### Branches

* before-otel: starting point for blog post guide

* main: with otel implemented
