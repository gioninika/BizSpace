import { Component } from '@angular/core';
import { ProductGridComponent } from '../../components/product-grid/product-grid.component';
import { CustomerRoutePage } from '../shared/customer-route-page';

@Component({
  selector: 'app-discover-page',
  standalone: true,
  imports: [ProductGridComponent],
  templateUrl: './discover-page.component.html',
  styleUrl: './discover-page.component.scss',
})
export class DiscoverPageComponent extends CustomerRoutePage {}
