import { Dialog } from '@base-ui/react/dialog';
import { act, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { handleBack } from './hardwareBack';

function OpenDialog() {
  return (
    <Dialog.Root defaultOpen>
      <Dialog.Portal>
        <Dialog.Popup>
          <Dialog.Title>Diagram</Dialog.Title>
        </Dialog.Popup>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

describe('handleBack', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    window.history.replaceState(null, '');
  });

  it('closes an open overlay before anything else', async () => {
    window.history.replaceState({ idx: 2 }, '');
    const back = vi.spyOn(window.history, 'back');
    await act(async () => {
      render(<OpenDialog />);
    });
    expect(screen.getByRole('dialog')).toBeInTheDocument();

    let result = '';
    await act(async () => {
      result = handleBack();
    });

    expect(result).toBe('closed');
    expect(back).not.toHaveBeenCalled();
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('goes back while the app has history', () => {
    window.history.replaceState({ idx: 1 }, '');
    const back = vi.spyOn(window.history, 'back').mockImplementation(() => {});

    expect(handleBack()).toBe('back');
    expect(back).toHaveBeenCalledOnce();
  });

  it('reports the first screen, so the app can leave', () => {
    window.history.replaceState({ idx: 0 }, '');
    expect(handleBack()).toBe('first');

    window.history.replaceState(null, '');
    expect(handleBack()).toBe('first');
  });
});
