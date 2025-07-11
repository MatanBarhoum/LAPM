import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, of } from 'rxjs';
import { AccessRequest } from '../models/access-request.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class RequestService {
  private adApiUrl = `https://control.lab.local/api/ad`;
  private requestsApiUrl = `https://control.lab.local/api/requests`;

  constructor(private http: HttpClient) { }

  checkComputerExists(name: string): Observable<boolean> {
    return this.http.get<{ exists: boolean }>(`${this.adApiUrl}/computer/${name}`, { withCredentials: true }).pipe(
      map(response => response.exists),
      catchError(() => of(false))
    );
  }

  checkUserExists(name: string): Observable<boolean> {
    return this.http.get<{ exists: boolean }>(`${this.adApiUrl}/user/${name}`, { withCredentials: true }).pipe(
      map(response => response.exists),
      catchError(() => of(false))
    );
  }

  createRequest(request: { computerName: string; domainUser: string; expirationTime: Date; notes?: string }): Observable<AccessRequest> {
    return this.http.post<AccessRequest>(this.requestsApiUrl, request, { withCredentials: true });
  }

  getMyRequests(): Observable<AccessRequest[]> {
    return this.http.get<AccessRequest[]>(`${this.requestsApiUrl}/mine`, { withCredentials: true });
  }
}