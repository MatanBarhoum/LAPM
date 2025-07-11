import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AccessRequest } from '../models/access-request.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private requestsApiUrl = `https://control.lab.local/api/requests`;

  constructor(private http: HttpClient) { }

  getAllRequests(): Observable<AccessRequest[]> {
    return this.http.get<AccessRequest[]>(this.requestsApiUrl, { withCredentials: true });
  }

  getPendingRequests(): Observable<AccessRequest[]> {
    return this.http.get<AccessRequest[]>(`${this.requestsApiUrl}/pending`, { withCredentials: true });
  }

  approveRequest(id: number): Observable<void> {
    return this.http.put<void>(`${this.requestsApiUrl}/${id}/approve`, {}, { withCredentials: true });
  }

  rejectRequest(id: number): Observable<void> {
    return this.http.put<void>(`${this.requestsApiUrl}/${id}/reject`, {}, { withCredentials: true });
  }

  revokeRequest(id: number): Observable<void> {
    return this.http.put<void>(`${this.requestsApiUrl}/${id}/revoke`, {}, { withCredentials: true });
  }

  extendRequest(id: number, newExpirationTime: Date): Observable<void> {
    return this.http.put<void>(`${this.requestsApiUrl}/${id}/extend`, { newExpirationTime }, { withCredentials: true });
  }
}
