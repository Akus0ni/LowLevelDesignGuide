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
    expect(fetchFn).toHaveBeenCalledWith('/api/info', expect.objectContaining({ method: 'GET' }));
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

describe('BridgeDataSource auth', () => {
  it('unwraps an ok BridgeResult (PascalCase from C#)', async () => {
    const bridge = {
      InvokeDotNet: vi.fn().mockResolvedValue({
        Ok: true,
        Value: {
          Id: 'u1',
          Email: 'a@example.com',
          DisplayName: 'A',
          Roles: ['Learner'],
          MustChangePassword: true,
        },
      }),
    };

    const user = await new BridgeDataSource(bridge).login('a@example.com', 'pw');

    expect(bridge.InvokeDotNet).toHaveBeenCalledWith('Login', ['a@example.com', 'pw']);
    expect(user).toEqual({
      id: 'u1',
      email: 'a@example.com',
      displayName: 'A',
      roles: ['Learner'],
      mustChangePassword: true,
    });
  });

  it('throws an AppError for a failed BridgeResult', async () => {
    const bridge = {
      InvokeDotNet: vi
        .fn()
        .mockResolvedValue({ ok: false, error: { code: 'auth.locked_out', message: 'Locked' } }),
    };

    await expect(new BridgeDataSource(bridge).login('a@example.com', 'pw')).rejects.toMatchObject({
      code: 'auth.locked_out',
      message: 'Locked',
    });
  });

  it('restoreSession returns null when C# has no session', async () => {
    const bridge = { InvokeDotNet: vi.fn().mockResolvedValue({ ok: true, value: null }) };

    await expect(new BridgeDataSource(bridge).restoreSession()).resolves.toBeNull();
  });
});
