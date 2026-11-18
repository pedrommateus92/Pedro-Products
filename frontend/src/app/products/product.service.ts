import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateProductRequest, Product, UpdateProductRequest } from './product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/products';

  getAll(): Observable<Product[]> {
    return this.http.get<Product[]>(this.baseUrl);
  }

  getById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, request);
  }

  update(id: number, request: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<unknown> {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }

  decrementStock(id: number, quantity: number): Observable<Product> {
    return this.http.post<Product>(`${this.baseUrl}/${id}/decrement-stock/${quantity}`, null);
  }

  addToStock(id: number, quantity: number): Observable<Product> {
    return this.http.post<Product>(`${this.baseUrl}/${id}/add-to-stock/${quantity}`, null);
  }

  searchByName(name: string): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/search`, { params: { name } });
  }

  getByStockLevel(min: number, max: number): Observable<Product[]> {
    const params = new HttpParams().set('min', min).set('max', max);
    return this.http.get<Product[]>(`${this.baseUrl}/stock-level`, { params });
  }
}
