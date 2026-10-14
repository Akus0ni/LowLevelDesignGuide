import { describe, expect, it, vi } from 'vitest';
import { BridgeDataSource } from './bridgeDataSource';
import { createDataSource } from './createDataSource';
import { HttpDataSource } from './httpDataSource';

describe('createDataSource', () => {
  it('uses HTTP in a plain browser', () => {
    expect(createDataSource({})).toBeInstanceOf(HttpDataSource);
  });

  it('uses the native bridge inside the MAUI app', () => {
    const bridge = { InvokeDotNet: vi.fn() };
    expect(createDataSource({ HybridWebView: bridge })).toBeInstanceOf(BridgeDataSource);
  });
});

describe('HttpDataSource', () => {
  it('reads app info from /api/info', async () => {
    const info = { name: 'LLD Guide API', version: '1.0.0', environment: 'Development' };
    const fetchFn = vi.fn().mockResolvedValue(new Response(JSON.stringify(info)));

    await expect(new HttpDataSource('', fetchFn).getInfo()).resolves.toEqual(info);
    expect(fetchFn).toHaveBeenCalledWith('/api/info');
  });

  it('throws on a failed response', async () => {
    const fetchFn = vi.fn().mockResolvedValue(new Response('', { status: 503 }));

    await expect(new HttpDataSource('', fetchFn).getInfo()).rejects.toThrow('503');
  });
});

describe('BridgeDataSource', () => {
  it('maps PascalCase results from C#', async () => {
    const bridge = {
      InvokeDotNet: vi
        .fn()
        .mockResolvedValue({ Name: 'LLD Guide', Version: '1.0.0', Environment: 'Android' }),
    };

    await expect(new BridgeDataSource(bridge).getInfo()).resolves.toEqual({
      name: 'LLD Guide',
      version: '1.0.0',
      environment: 'Android',
    });
    expect(bridge.InvokeDotNet).toHaveBeenCalledWith('GetInfo');
  });
});
