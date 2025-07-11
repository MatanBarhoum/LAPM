import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { AccessRequest } from '../../../core/models/access-request.model';
import { BehaviorSubject, switchMap } from 'rxjs';

@Component({
  selector: 'app-pending-requests',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pending-requests.component.html',
})
export class PendingRequestsComponent implements OnInit {
  
  private refreshSubject = new BehaviorSubject<void>(undefined);
  pendingRequests$ = this.refreshSubject.pipe(
    switchMap(() => this.adminService.getPendingRequests())
  );

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {}

  approve(id: number): void {
    this.adminService.approveRequest(id).subscribe(() => this.refreshSubject.next());
  }

  reject(id: number): void {
    this.adminService.rejectRequest(id).subscribe(() => this.refreshSubject.next());
  }
}