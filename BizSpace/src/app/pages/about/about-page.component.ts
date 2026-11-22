import { Component } from '@angular/core';
import { CustomerRoutePage } from '../shared/customer-route-page';

@Component({
  selector: 'app-about-page',
  standalone: true,
  templateUrl: './about-page.component.html',
  styleUrl: './about-page.component.scss',
})
export class AboutPageComponent extends CustomerRoutePage {}
