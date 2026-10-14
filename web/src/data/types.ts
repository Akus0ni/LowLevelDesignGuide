/** Mirrors Lld.Contracts.AppInfo. */
export interface AppInfo {
  name: string;
  version: string;
  environment: string;
}

/**
 * The only way UI code reads or writes data. Two strategies implement it:
 * HttpDataSource (browser, talks to the API) and BridgeDataSource (inside the MAUI app).
 */
export interface DataSource {
  readonly kind: 'http' | 'bridge';
  getInfo(): Promise<AppInfo>;
}

/** Shape of window.HybridWebView, injected by _framework/hybridwebview.js inside the MAUI app. */
export interface HybridWebViewBridge {
  InvokeDotNet(methodName: string, paramValues?: unknown[]): Promise<unknown>;
}

declare global {
  interface Window {
    HybridWebView?: HybridWebViewBridge;
  }
}
