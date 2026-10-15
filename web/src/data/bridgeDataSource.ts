import {
  AppError,
  type AccessRequestInput,
  type AppInfo,
  type DataSource,
  type HybridWebViewBridge,
  type UserSummary,
} from './types';

type Raw = Record<string, unknown>;

/** C# may serialise with PascalCase; read either casing. */
function pick<T>(raw: Raw, camel: string): T {
  const pascal = camel[0].toUpperCase() + camel.slice(1);
  return (raw[camel] ?? raw[pascal]) as T;
}

function parse(raw: unknown): Raw {
  return (typeof raw === 'string' ? JSON.parse(raw) : raw) as Raw;
}

function toAppInfo(raw: unknown): AppInfo {
  const value = parse(raw);
  return {
    name: pick(value, 'name'),
    version: pick(value, 'version'),
    environment: pick(value, 'environment'),
  };
}

function toUser(raw: Raw): UserSummary {
  return {
    id: pick(raw, 'id'),
    email: pick(raw, 'email'),
    displayName: pick(raw, 'displayName'),
    roles: pick(raw, 'roles') ?? [],
    mustChangePassword: Boolean(pick(raw, 'mustChangePassword')),
  };
}

/**
 * MAUI strategy: every call goes to BridgeService in C#, which holds the tokens in SecureStorage and talks
 * to the API. C# answers with a BridgeResult { ok, value, error { code, message } } instead of throwing.
 */
export class BridgeDataSource implements DataSource {
  readonly kind = 'bridge';
  private readonly bridge: HybridWebViewBridge;

  constructor(bridge: HybridWebViewBridge) {
    this.bridge = bridge;
  }

  async getInfo(): Promise<AppInfo> {
    return toAppInfo(await this.bridge.InvokeDotNet('GetInfo'));
  }

  async requestAccess(input: AccessRequestInput): Promise<void> {
    await this.call('RequestAccess', [input.email, input.name, input.note ?? null]);
  }

  async login(email: string, password: string): Promise<UserSummary> {
    return toUser((await this.call('Login', [email, password])) as Raw);
  }

  async restoreSession(): Promise<UserSummary | null> {
    const value = await this.call('RestoreSession');
    return value ? toUser(value as Raw) : null;
  }

  async changePassword(currentPassword: string, newPassword: string): Promise<UserSummary> {
    return toUser((await this.call('ChangePassword', [currentPassword, newPassword])) as Raw);
  }

  async logout(): Promise<void> {
    await this.call('Logout');
  }

  async logoutAll(): Promise<void> {
    await this.call('LogoutAll');
  }

  private async call(method: string, args: unknown[] = []): Promise<unknown> {
    const result = parse(await this.bridge.InvokeDotNet(method, args));
    if (pick<boolean>(result, 'ok')) return pick(result, 'value');

    const error = (pick<Raw>(result, 'error') ?? {}) as Raw;
    throw new AppError(
      pick(error, 'code') ?? 'bridge.failed',
      pick(error, 'message') ?? 'Something went wrong.',
    );
  }
}
