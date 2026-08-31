import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api.config';
import { DayPlan, DayPlanInput, DayRoster, IsoDate, TeamSchedule } from './api.models';

@Injectable({ providedIn: 'root' })
export class ScheduleApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getSchedule(teamId: string, from: IsoDate, to: IsoDate): Observable<TeamSchedule> {
    return this.http.get<TeamSchedule>(`${this.baseUrl}/teams/${teamId}/schedule`, {
      params: new HttpParams({ fromObject: { from, to } }),
    });
  }

  getDayRoster(teamId: string, date: IsoDate): Observable<DayRoster> {
    return this.http.get<DayRoster>(`${this.baseUrl}/teams/${teamId}/schedule/${date}`);
  }

  getMyPlan(teamId: string, from: IsoDate, to: IsoDate): Observable<DayPlan[]> {
    return this.http.get<DayPlan[]>(`${this.baseUrl}/teams/${teamId}/attendance/me`, {
      params: new HttpParams({ fromObject: { from, to } }),
    });
  }

  setMyPlan(teamId: string, days: readonly DayPlanInput[]): Observable<DayPlan[]> {
    return this.http.put<DayPlan[]>(`${this.baseUrl}/teams/${teamId}/attendance/me`, { days });
  }

  clearMyPlan(teamId: string, dates: readonly IsoDate[]): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/teams/${teamId}/attendance/me/clear`, { dates });
  }
}
