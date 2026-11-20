import { Product } from '../app/products/product.model';

export function mockProduct(overrides: Partial<Product> = {}): Product {
  return {
    id: 100000,
    name: 'Mechanical Keyboard',
    description: 'Tenkeyless keyboard',
    price: 89.9,
    stock: 25,
    rowVersion: 'AAAAAAAAB9E=',
    ...overrides,
  };
}
