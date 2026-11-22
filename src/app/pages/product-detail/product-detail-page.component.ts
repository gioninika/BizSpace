import { Component } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { CustomerRoutePage } from '../shared/customer-route-page';

@Component({
  selector: 'app-product-detail-page',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './product-detail-page.component.html',
  styleUrl: './product-detail-page.component.scss',
})
export class ProductDetailPageComponent extends CustomerRoutePage {}
