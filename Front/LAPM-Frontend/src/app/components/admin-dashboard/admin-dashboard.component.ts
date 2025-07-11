import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../core/services/admin.service';
import { AccessRequest } from '../../core/models/access-request.model';
import { BehaviorSubject, switchMap } from 'rxjs';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-dashboard.component.html',
})
export class AdminDashboardComponent implements OnInit {
  
  // A subject to trigger a refresh of the request list
  private refreshSubject = new BehaviorSubject<void>(undefined);
  
  // Observable stream of all requests, re-fetched whenever refreshSubject emits
  allRequests$ = this.refreshSubject.pipe(
    switchMap(() => this.adminService.getAllRequests())
  );

  statusColors: { [key: string]: string } = {
    'Pending': 'bg-yellow-100 text-yellow-800', 'Approved': 'bg-blue-100 text-blue-800',
    'Applied': 'bg-green-100 text-green-800', 'Rejected': 'bg-red-100 text-red-800',
    'Expired': 'bg-gray-100 text-gray-800', 'Revoked': 'bg-purple-100 text-purple-800',
  };

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {}

  // --- Action Methods ---

  approve(id: number): void {
    this.adminService.approveRequest(id).subscribe(() => this.refreshList());
  }

  reject(id: number): void {
    this.adminService.rejectRequest(id).subscribe(() => this.refreshList());
  }

  revoke(id: number): void {
    // Using a simple confirm dialog for this destructive action
    if (confirm('Are you sure you want to revoke this grant immediately? This action cannot be undone.')) {
      this.adminService.revokeRequest(id).subscribe(() => this.refreshList());
    }
  }

  extend(id: number): void {
    const hours = prompt('How many additional hours do you want to grant access for?', '1');
    if (hours && !isNaN(+hours) && +hours > 0) {
      const newExpirationTime = new Date();
      newExpirationTime.setHours(newExpirationTime.getHours() + parseInt(hours, 10));
      this.adminService.extendRequest(id, newExpirationTime).subscribe(() => this.refreshList());
    } else if (hours !== null) {
      alert('Please enter a valid number of hours.');
    }
  }

  private refreshList(): void {
    this.refreshSubject.next();
  }
}
