import type { AppInfo, DataSource } from './types';

type Fetch = typeof fetch;

export class HttpDataSource implements DataSource {
  readonly kind = 'http';
  private readonly baseUrl: string;
  private readonly fetchFn: Fetch;

  constructor(baseUrl = '', fetchFn: Fetch = (...args) => fetch(...args)) {
    this.baseUrl = baseUrl;
    this.fetchFn = fetchFn;
  }

  async getInfo(): Promise<AppInfo> {
    const response = await this.fetchFn(`${this.baseUrl}/api/info`);
    if (!response.ok) {
      throw new Error(`GET /api/info failed with ${response.status}`);
    }
    return (await response.json()) as AppInfo;
  }
}
