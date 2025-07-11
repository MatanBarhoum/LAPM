import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RequestService } from '../../core/services/request.service';
import { AccessRequest } from '../../core/models/access-request.model';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-my-requests',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './my-requests.component.html',
  styleUrls: ['./my-requests.component.css']
})
export class MyRequestsComponent implements OnInit {
  myRequests$!: Observable<AccessRequest[]>;
  statusColors: { [key: string]: string } = {
    'Pending': 'bg-yellow-100 text-yellow-800',
    'Approved': 'bg-blue-100 text-blue-800',
    'Applied': 'bg-green-100 text-green-800',
    'Rejected': 'bg-red-100 text-red-800',
    'Expired': 'bg-gray-100 text-gray-800',
    'Revoked': 'bg-purple-100 text-purple-800',
  };

  constructor(private requestService: RequestService) {}

  ngOnInit(): void {
    this.myRequests$ = this.requestService.getMyRequests();
  }
}