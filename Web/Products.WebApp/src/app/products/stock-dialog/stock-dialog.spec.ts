import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { mockProduct } from '../../../testing/test-data';
import { StockDialog, StockDialogData, StockOperation } from './stock-dialog';

async function setup(operation: StockOperation, stock = 2) {
  const close = vi.fn();
  const data: StockDialogData = { product: mockProduct({ stock }), operation };
  await render(StockDialog, {
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: MAT_DIALOG_DATA, useValue: data },
      { provide: MatDialogRef, useValue: { close } },
    ],
  });
  return {
    close,
    http: TestBed.inject(HttpTestingController),
    user: userEvent.setup(),
    quantity: screen.getByRole('spinbutton', { name: /quantity/i }),
  };
}

describe('StockDialog', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('shows the current stock', async () => {
    await setup('remove', 2);

    expect(screen.getByText(/currently has/)).toHaveTextContent(
      'Mechanical Keyboard currently has 2 in stock.',
    );
  });

  it('blocks removing more than the current stock', async () => {
    const { user, quantity } = await setup('remove', 2);

    await user.type(quantity, '3');
    await user.click(screen.getByRole('button', { name: 'Remove' }));

    expect(screen.getByText('Only 2 in stock.')).toBeInTheDocument();
  });

  it.each(['0', '1.5'])('rejects a quantity of %s', async (value) => {
    const { user, quantity } = await setup('add');

    await user.type(quantity, value);
    await user.click(screen.getByRole('button', { name: 'Add' }));

    expect(screen.getByText('Enter a whole number of 1 or more.')).toBeInTheDocument();
  });

  it('adds stock and closes with the updated product', async () => {
    const { close, http, user, quantity } = await setup('add', 2);

    await user.type(quantity, '5');
    await user.click(screen.getByRole('button', { name: 'Add' }));

    const updated = mockProduct({ stock: 7 });
    http.expectOne({ method: 'POST', url: '/api/products/100000/add-to-stock/5' }).flush(updated);

    expect(close).toHaveBeenCalledWith(updated);
  });

  it('shows the API message when the stock changed in the meantime', async () => {
    const { close, http, user, quantity } = await setup('remove', 2);

    await user.type(quantity, '2');
    await user.click(screen.getByRole('button', { name: 'Remove' }));

    http.expectOne({ method: 'POST', url: '/api/products/100000/decrement-stock/2' }).flush(
      {
        title: 'Conflict',
        detail: 'Insufficient stock to remove 2 unit(s) from product 100000.',
      },
      { status: 409, statusText: 'Conflict' },
    );

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Insufficient stock to remove 2 unit(s)',
    );
    expect(close).not.toHaveBeenCalled();
  });
});
