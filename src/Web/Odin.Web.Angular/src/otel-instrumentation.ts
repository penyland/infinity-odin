import { EnvironmentProviders, provideAppInitializer } from '@angular/core';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';

export function provideOpenTelemetryInstrumentation(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const resource = resourceFromAttributes({
        [ATTR_SERVICE_NAME]: 'Odin-Web-Angular',
        [ATTR_SERVICE_VERSION]: '1.0.0',
      })
   
  });
}