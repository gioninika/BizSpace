import { Component } from '@angular/core';
import { CustomerRoutePage } from '../shared/customer-route-page';

@Component({
  selector: 'app-my-orders-page',
  standalone: true,
  templateUrl: './my-orders-page.component.html',
  styleUrl: './my-orders-page.component.scss',
})
export class MyOrdersPageComponent extends CustomerRoutePage {}
