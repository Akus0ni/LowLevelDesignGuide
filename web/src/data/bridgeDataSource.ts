import type { AppInfo, DataSource, HybridWebViewBridge } from './types';

/** C# may serialise with PascalCase; accept either casing. */
function toAppInfo(raw: unknown): AppInfo {
  const value = (typeof raw === 'string' ? JSON.parse(raw) : raw) as Record<string, string>;
  return {
    name: value.name ?? value.Name,
    version: value.version ?? value.Version,
    environment: value.environment ?? value.Environment,
  };
}

export class BridgeDataSource implements DataSource {
  readonly kind = 'bridge';

  private readonly bridge: HybridWebViewBridge;

  constructor(bridge: HybridWebViewBridge) {
    this.bridge = bridge;
  }

  async getInfo(): Promise<AppInfo> {
    return toAppInfo(await this.bridge.InvokeDotNet('GetInfo'));
  }
}
