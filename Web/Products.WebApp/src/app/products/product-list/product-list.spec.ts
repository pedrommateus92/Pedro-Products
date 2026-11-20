import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { render, screen, waitFor, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { mockProduct } from '../../../testing/test-data';
import { Product } from '../product.model';
import { ProductList } from './product-list';

const keyboard = mockProduct({ id: 100000, name: 'Mechanical Keyboard', stock: 25 });
const mouse = mockProduct({ id: 100001, name: 'Wireless Mouse', stock: 60 });
const hub = mockProduct({ id: 100003, name: 'USB-C Hub', stock: 0 });

async function setup() {
  await render(ProductList, { providers: [provideHttpClient(), provideHttpClientTesting()] });
  return { http: TestBed.inject(HttpTestingController), user: userEvent.setup() };
}

function rowNames(): string[] {
  return screen
    .getAllByRole('row')
    .slice(1)
    .map((row) => within(row).getAllByRole('cell')[1].textContent?.trim() ?? '');
}

function many(count: number): Product[] {
  return Array.from({ length: count }, (_, i) =>
    mockProduct({ id: 100000 + i, name: `Product ${i + 1}`, stock: i }),
  );
}

describe('ProductList', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('lists the products with their stock', async () => {
    const { http } = await setup();

    http.expectOne('/api/products').flush([keyboard, mouse, hub]);

    expect(await screen.findByText('Mechanical Keyboard')).toBeInTheDocument();
    expect(rowNames()).toEqual(['Mechanical Keyboard', 'Wireless Mouse', 'USB-C Hub']);
    const hubRow = screen.getByText('USB-C Hub').closest('tr') as HTMLElement;
    expect(within(hubRow).getByText('0')).toBeInTheDocument();
    expect(
      within(hubRow).getByRole('button', { name: 'Remove stock from USB-C Hub' }),
    ).toBeDisabled();
  });

  it('shows an empty state when there are no products', async () => {
    const { http } = await setup();

    http.expectOne('/api/products').flush([]);

    expect(await screen.findByText('No products yet. Create the first one.')).toBeInTheDocument();
  });

  it('shows an error with a working Retry button', async () => {
    const { http, user } = await setup();

    http.expectOne('/api/products').flush(null, { status: 500, statusText: 'Server Error' });
    expect(await screen.findByRole('alert')).toHaveTextContent('Could not load products.');

    await user.click(screen.getByRole('button', { name: 'Retry' }));
    http.expectOne('/api/products').flush([keyboard]);

    expect(await screen.findByText('Mechanical Keyboard')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('searches by name after the user stops typing', async () => {
    const { http, user } = await setup();
    http.expectOne('/api/products').flush([keyboard, mouse]);

    await user.type(screen.getByRole('textbox', { name: /search by name/i }), 'key');

    const request = await waitFor(() => http.expectOne((r) => r.url === '/api/products/search'));
    expect(request.request.params.get('name')).toBe('key');
    request.flush([keyboard]);

    await waitFor(() => expect(rowNames()).toEqual(['Mechanical Keyboard']));
  });

  it('filters by stock level', async () => {
    const { http, user } = await setup();
    http.expectOne('/api/products').flush([keyboard, mouse, hub]);

    await user.type(screen.getByRole('spinbutton', { name: /min stock/i }), '1');
    await user.type(screen.getByRole('spinbutton', { name: /max stock/i }), '30');

    const request = await waitFor(() =>
      http.expectOne((r) => r.url === '/api/products/stock-level'),
    );
    expect(request.request.params.get('min')).toBe('1');
    expect(request.request.params.get('max')).toBe('30');
    request.flush([keyboard]);

    await waitFor(() => expect(rowNames()).toEqual(['Mechanical Keyboard']));
  });

  it('applies the stock range to search results when both are set', async () => {
    const { http, user } = await setup();
    http.expectOne('/api/products').flush([keyboard, mouse, hub]);

    await user.type(screen.getByRole('spinbutton', { name: /min stock/i }), '30');
    await user.type(screen.getByRole('textbox', { name: /search by name/i }), 'e');

    const request = await waitFor(() => http.expectOne((r) => r.url === '/api/products/search'));
    request.flush([keyboard, mouse]);

    await waitFor(() => expect(rowNames()).toEqual(['Wireless Mouse']));
  });

  it('does not query while min stock is greater than max stock', async () => {
    const { http, user } = await setup();
    http.expectOne('/api/products').flush([keyboard]);

    await user.type(screen.getByRole('spinbutton', { name: /min stock/i }), '10');
    await user.type(screen.getByRole('spinbutton', { name: /max stock/i }), '5');

    expect(
      await screen.findByText('Min stock cannot be greater than max stock.'),
    ).toBeInTheDocument();
    await new Promise((resolve) => setTimeout(resolve, 400));
    http.expectNone((r) => r.url.startsWith('/api/products/'));
  });

  it('pages through the results', async () => {
    const { http, user } = await setup();
    http.expectOne('/api/products').flush(many(12));

    await screen.findByText('Product 1');
    expect(rowNames()).toHaveLength(10);
    expect(screen.getByText('1 – 10 of 12')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Next page' }));

    expect(rowNames()).toEqual(['Product 11', 'Product 12']);
  });

  it('deletes a product after confirmation and reloads the list', async () => {
    const { http, user } = await setup();
    http.expectOne('/api/products').flush([keyboard, mouse]);

    await user.click(await screen.findByRole('button', { name: 'Delete Mechanical Keyboard' }));
    const dialog = await screen.findByRole('dialog');
    expect(dialog).toHaveTextContent(
      'Delete "Mechanical Keyboard" (ID 100000)? This cannot be undone.',
    );
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }));

    // The dialog reports its result after its close animation, so wait for the request.
    const deleteRequest = await waitFor(() =>
      http.expectOne({ method: 'DELETE', url: '/api/products/100000' }),
    );
    deleteRequest.flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne('/api/products').flush([mouse]);

    await waitFor(() => expect(rowNames()).toEqual(['Wireless Mouse']));
  });

  it('does not delete when the confirmation is cancelled', async () => {
    const { http, user } = await setup();
    http.expectOne('/api/products').flush([keyboard]);

    await user.click(await screen.findByRole('button', { name: 'Delete Mechanical Keyboard' }));
    await user.click(
      within(await screen.findByRole('dialog')).getByRole('button', { name: 'Cancel' }),
    );

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
    http.expectNone({ method: 'DELETE' });
  });
});
