import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AccountDto,
  AskResponse,
  CategoryDto,
  JobAccepted,
  LedgerFilters,
  MonthlySummary,
  PagedResult,
  ParseResponse,
  PatchTransactionRequest,
  RuleDto,
  SmsSyncResult,
  TransactionDto,
  TrendPoint,
  UpsertAccountRequest,
} from '../models/api-models';

/** Typed client for the Masroof API. One method per endpoint in the contract. */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  parse(text: string): Observable<ParseResponse> {
    return this.http.post<ParseResponse>(`${this.base}/transactions/parse`, { text });
  }

  parseBatch(text: string): Observable<JobAccepted> {
    return this.http.post<JobAccepted>(`${this.base}/transactions/parse-batch`, { text });
  }

  /** Import this month's bank/wallet SMS from the device inbox into the ledger. */
  syncSms(): Observable<SmsSyncResult> {
    return this.http.post<SmsSyncResult>(`${this.base}/transactions/sync-sms`, {});
  }

  getLedger(filters: LedgerFilters): Observable<PagedResult<TransactionDto>> {
    let params = new HttpParams();
    if (filters.month) params = params.set('month', filters.month);
    if (filters.category) params = params.set('category', filters.category);
    if (filters.direction) params = params.set('direction', filters.direction);
    if (filters.accountId != null) params = params.set('accountId', filters.accountId);
    if (filters.q) params = params.set('q', filters.q);
    if (filters.needsReview) params = params.set('needsReview', true);
    params = params.set('page', filters.page ?? 1).set('pageSize', filters.pageSize ?? 50);
    return this.http.get<PagedResult<TransactionDto>>(`${this.base}/transactions`, { params });
  }

  patchTransaction(id: number, body: PatchTransactionRequest): Observable<TransactionDto> {
    return this.http.patch<TransactionDto>(`${this.base}/transactions/${id}`, body);
  }

  deleteTransaction(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/transactions/${id}`);
  }

  getSummary(month?: string): Observable<MonthlySummary> {
    let params = new HttpParams();
    if (month) params = params.set('month', month);
    return this.http.get<MonthlySummary>(`${this.base}/reports/summary`, { params });
  }

  getTrend(months = 6): Observable<TrendPoint[]> {
    return this.http.get<TrendPoint[]>(`${this.base}/reports/trend`, {
      params: new HttpParams().set('months', months),
    });
  }

  ask(question: string): Observable<AskResponse> {
    return this.http.post<AskResponse>(`${this.base}/ask`, { question });
  }

  getRules(q?: string): Observable<RuleDto[]> {
    let params = new HttpParams();
    if (q) params = params.set('q', q);
    return this.http.get<RuleDto[]>(`${this.base}/rules`, { params });
  }

  deleteRule(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/rules/${id}`);
  }

  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(`${this.base}/categories`);
  }

  getAccounts(): Observable<AccountDto[]> {
    return this.http.get<AccountDto[]>(`${this.base}/accounts`);
  }

  createAccount(body: UpsertAccountRequest): Observable<AccountDto> {
    return this.http.post<AccountDto>(`${this.base}/accounts`, body);
  }

  updateAccount(id: number, body: UpsertAccountRequest): Observable<AccountDto> {
    return this.http.patch<AccountDto>(`${this.base}/accounts/${id}`, body);
  }
}
