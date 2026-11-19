import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { Product } from './product.model';
import { ProductService } from './product.service';

const product: Product = {
  id: 100000,
  name: 'Keyboard',
  description: null,
  price: 89.9,
  stock: 25,
  rowVersion: 'AAAAAAAAB9E=',
};

describe('ProductService', () => {
  let service: ProductService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProductService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gets all products', async () => {
    const result = firstValueFrom(service.getAll());

    const request = http.expectOne('/api/products');
    expect(request.request.method).toBe('GET');
    request.flush([product]);

    expect(await result).toEqual([product]);
  });

  it('gets a product by id', async () => {
    const result = firstValueFrom(service.getById(100000));

    http.expectOne({ method: 'GET', url: '/api/products/100000' }).flush(product);

    expect(await result).toEqual(product);
  });

  it('creates a product with a POST body', async () => {
    const body = { name: 'Keyboard', description: null, price: 89.9, stock: 25 };
    const result = firstValueFrom(service.create(body));

    const request = http.expectOne({ method: 'POST', url: '/api/products' });
    expect(request.request.body).toEqual(body);
    request.flush(product);

    expect(await result).toEqual(product);
  });

  it('updates a product with its row version', async () => {
    const body = {
      name: 'Keyboard',
      description: null,
      price: 79.9,
      stock: 25,
      rowVersion: 'AAAAAAAAB9E=',
    };
    const result = firstValueFrom(service.update(100000, body));

    const request = http.expectOne({ method: 'PUT', url: '/api/products/100000' });
    expect(request.request.body).toEqual(body);
    request.flush({ ...product, price: 79.9 });

    expect((await result).price).toBe(79.9);
  });

  it('deletes a product', async () => {
    const result = firstValueFrom(service.delete(100000));

    http
      .expectOne({ method: 'DELETE', url: '/api/products/100000' })
      .flush(null, { status: 204, statusText: 'No Content' });

    expect(await result).toBeNull();
  });

  it('decrements and adds stock with the quantity in the route', async () => {
    const decremented = firstValueFrom(service.decrementStock(100000, 3));
    http
      .expectOne({ method: 'POST', url: '/api/products/100000/decrement-stock/3' })
      .flush({ ...product, stock: 22 });

    const added = firstValueFrom(service.addToStock(100000, 5));
    http
      .expectOne({ method: 'POST', url: '/api/products/100000/add-to-stock/5' })
      .flush({ ...product, stock: 27 });

    expect((await decremented).stock).toBe(22);
    expect((await added).stock).toBe(27);
  });

  it('searches by name with an encoded query parameter', async () => {
    const result = firstValueFrom(service.searchByName('50% off'));

    const request = http.expectOne((r) => r.url === '/api/products/search');
    expect(request.request.params.get('name')).toBe('50% off');
    expect(request.request.urlWithParams).toBe('/api/products/search?name=50%25%20off');
    request.flush([product]);

    expect(await result).toEqual([product]);
  });

  it('filters by stock level with min and max parameters', async () => {
    const result = firstValueFrom(service.getByStockLevel(0, 10));

    const request = http.expectOne((r) => r.url === '/api/products/stock-level');
    expect(request.request.params.get('min')).toBe('0');
    expect(request.request.params.get('max')).toBe('10');
    request.flush([]);

    expect(await result).toEqual([]);
  });
});
