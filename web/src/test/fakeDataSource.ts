import { vi } from 'vitest';
import { AppError, type DataSource, type UserSummary } from '../data/types';

export const learner: UserSummary = {
  id: 'user-1',
  email: 'learner@example.com',
  displayName: 'Ada Learner',
  roles: ['Learner'],
  mustChangePassword: false,
};

/** In-memory DataSource for component tests; every method is a spy. */
export function fakeDataSource(overrides: Partial<DataSource> = {}): DataSource {
  return {
    kind: 'bridge',
    getInfo: vi.fn().mockResolvedValue({ name: 'LLD Guide', version: '0.1.0', environment: 'Test' }),
    requestAccess: vi.fn().mockResolvedValue(undefined),
    login: vi
      .fn()
      .mockRejectedValue(new AppError('auth.invalid_credentials', 'Email or password is incorrect.', 401)),
    restoreSession: vi.fn().mockResolvedValue(null),
    changePassword: vi.fn().mockResolvedValue(learner),
    logout: vi.fn().mockResolvedValue(undefined),
    logoutAll: vi.fn().mockResolvedValue(undefined),
    ...overrides,
  };
}
