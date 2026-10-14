import { BridgeDataSource } from './bridgeDataSource';
import { HttpDataSource } from './httpDataSource';
import type { DataSource } from './types';

/** Factory: inside the MAUI shell use the native bridge, otherwise call the API over HTTP. */
export function createDataSource(win: Pick<Window, 'HybridWebView'> = window): DataSource {
  return win.HybridWebView ? new BridgeDataSource(win.HybridWebView) : new HttpDataSource();
}
