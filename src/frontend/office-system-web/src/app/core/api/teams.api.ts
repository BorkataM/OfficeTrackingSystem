import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';
import { TeamDetail, TeamListItem, TeamRole } from './api.models';

@Injectable({ providedIn: 'root' })
export class TeamsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/teams`;

  list(): Observable<TeamListItem[]> {
    return this.http.get<TeamListItem[]>(`${this.baseUrl}/`);
  }

  get(teamId: string): Observable<TeamDetail> {
    return this.http.get<TeamDetail>(`${this.baseUrl}/${teamId}`);
  }

  create(name: string, description: string | null): Observable<TeamDetail> {
    return this.http.post<TeamDetail>(`${this.baseUrl}/`, { name, description });
  }

  join(joinCode: string): Observable<TeamDetail> {
    return this.http.post<TeamDetail>(`${this.baseUrl}/join`, { joinCode });
  }

  update(teamId: string, name: string, description: string | null): Observable<TeamDetail> {
    return this.http.put<TeamDetail>(`${this.baseUrl}/${teamId}`, { name, description });
  }

  delete(teamId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${teamId}`);
  }

  regenerateJoinCode(teamId: string): Observable<string> {
    return this.http.post(`${this.baseUrl}/${teamId}/join-code`, null, { responseType: 'text' });
  }

  leave(teamId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${teamId}/leave`, null);
  }

  changeRole(teamId: string, userId: string, role: TeamRole): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${teamId}/members/${userId}/role`, { role });
  }

  transferOwnership(teamId: string, userId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${teamId}/members/${userId}/ownership`, null);
  }

  removeMember(teamId: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${teamId}/members/${userId}`);
  }
}
