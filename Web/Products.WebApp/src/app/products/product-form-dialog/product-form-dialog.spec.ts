import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { aProduct } from '../../../testing/test-data';
import { Product } from '../product.model';
import { ProductFormDialog } from './product-form-dialog';

async function setup(product: Product | null = null) {
  const close = vi.fn();
  await render(ProductFormDialog, {
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: MAT_DIALOG_DATA, useValue: { product } },
      { provide: MatDialogRef, useValue: { close } },
    ],
  });
  return { close, http: TestBed.inject(HttpTestingController), user: userEvent.setup() };
}

describe('ProductFormDialog', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('shows required-field errors and sends nothing when the form is empty', async () => {
    const { user } = await setup();

    await user.click(screen.getByRole('button', { name: 'Create' }));

    expect(screen.getByText('Name is required.')).toBeInTheDocument();
    expect(screen.getByText('Price is required.')).toBeInTheDocument();
    expect(screen.getByText('Stock is required.')).toBeInTheDocument();
  });

  it('rejects a price with more than 2 decimals', async () => {
    const { user } = await setup();

    await user.type(screen.getByRole('spinbutton', { name: /price/i }), '9.999');
    await user.tab();

    expect(screen.getByText('Use at most 2 decimal places.')).toBeInTheDocument();
  });

  it('creates a product with trimmed values and closes with the result', async () => {
    const { close, http, user } = await setup();

    await user.type(screen.getByRole('textbox', { name: /name/i }), '  Desk Lamp  ');
    await user.type(screen.getByRole('spinbutton', { name: /price/i }), '24.99');
    await user.type(screen.getByRole('spinbutton', { name: /stock/i }), '12');
    await user.click(screen.getByRole('button', { name: 'Create' }));

    const request = http.expectOne({ method: 'POST', url: '/api/products' });
    expect(request.request.body).toEqual({
      name: 'Desk Lamp',
      description: null,
      price: 24.99,
      stock: 12,
    });
    const created = aProduct({ id: 100007, name: 'Desk Lamp', price: 24.99, stock: 12 });
    request.flush(created);

    expect(close).toHaveBeenCalledWith(created);
  });

  it('shows server validation errors under the matching field', async () => {
    const { close, http, user } = await setup();

    await user.type(screen.getByRole('textbox', { name: /name/i }), 'Lamp');
    await user.type(screen.getByRole('spinbutton', { name: /price/i }), '1');
    await user.type(screen.getByRole('spinbutton', { name: /stock/i }), '1');
    await user.click(screen.getByRole('button', { name: 'Create' }));

    http
      .expectOne('/api/products')
      .flush(
        { title: 'One or more validation errors occurred.', errors: { name: ['Name is taken.'] } },
        { status: 400, statusText: 'Bad Request' },
      );

    expect(await screen.findByText('Name is taken.')).toBeInTheDocument();
    expect(close).not.toHaveBeenCalled();
  });

  it('prefills the form when editing and sends the row version', async () => {
    const product = aProduct();
    const { close, http, user } = await setup(product);

    expect(screen.getByRole('heading', { name: 'Edit product' })).toBeInTheDocument();
    const name = screen.getByRole('textbox', { name: /name/i });
    expect(name).toHaveValue('Mechanical Keyboard');

    await user.clear(name);
    await user.type(name, 'Keyboard Pro');
    await user.click(screen.getByRole('button', { name: 'Save changes' }));

    const request = http.expectOne({ method: 'PUT', url: '/api/products/100000' });
    expect(request.request.body).toEqual({
      name: 'Keyboard Pro',
      description: 'Tenkeyless keyboard',
      price: 89.9,
      stock: 25,
      rowVersion: 'AAAAAAAAB9E=',
    });
    request.flush({ ...product, name: 'Keyboard Pro' });

    expect(close).toHaveBeenCalledWith(expect.objectContaining({ name: 'Keyboard Pro' }));
  });

  it('explains an edit conflict and disables saving', async () => {
    const { close, http, user } = await setup(aProduct());

    await user.click(screen.getByRole('button', { name: 'Save changes' }));
    http
      .expectOne('/api/products/100000')
      .flush(
        { title: 'Conflict', detail: 'Changed by someone else.' },
        { status: 409, statusText: 'Conflict' },
      );

    expect(await screen.findByRole('alert')).toHaveTextContent('Someone else changed this product');
    expect(screen.getByRole('button', { name: 'Save changes' })).toBeDisabled();
    expect(close).not.toHaveBeenCalled();
  });
});
