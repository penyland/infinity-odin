export const environment = {
  production: false,
  appVersion: import.meta.env.NG_APP_VERSION,
  myVar1: import.meta.env.NG_APP_MY_VAR1,
  myVar2: import.meta.env.NG_APP_MY_VAR2,
  otelExporterOtlpEndpoint: import.meta.env.OTEL_EXPORTER_OTLP_ENDPOINT,
  otelExporterOtlpHeaders: import.meta.env.OTEL_EXPORTER_OTLP_HEADERS,
};
