export interface Product {
  id: number;
  name: string;
  description: string | null;
  price: number;
  stock: number;
}

export interface CreateProductRequest {
  name: string;
  description: string | null;
  price: number;
  stock: number;
}

export type UpdateProductRequest = CreateProductRequest;
