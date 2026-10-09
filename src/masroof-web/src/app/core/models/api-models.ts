// Mirrors the Masroof API DTOs (System.Text.Json camelCase).

export type Direction = 'debit' | 'credit';
export type Source = 'preparser' | 'llm' | 'manual';

export interface CategoryRef {
  code: string;
  name: string;
}

export interface TransactionDto {
  id: number;
  direction: Direction;
  amount: number;
  currency: string;
  counterparty: string | null;
  channel: string | null;
  txnDate: string; // yyyy-MM-dd
  category: CategoryRef;
  confidence: number | null;
  source: Source;
  isCorrected: boolean;
  createdAt: string;
  transferGroupId: string | null;
  transferFrom: string | null;
  transferTo: string | null;
}

export interface ParseResponse {
  id: number;
  direction: Direction;
  amount: number;
  currency: string;
  counterparty: string | null;
  channel: string | null;
  txnDate: string;
  category: CategoryRef;
  confidence: number | null;
  source: Source;
  hintsUsed: string[];
  latencyMs: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export interface CategoryDto {
  code: string;
  name: string;
  icon: string | null;
  color: string | null;
}

export interface CategoryTotal {
  categoryCode: string;
  categoryName: string;
  total: number;
  count: number;
}

export interface MonthlySummary {
  month: string;
  totalDebit: number;
  totalCredit: number;
  transactionCount: number;
  previousMonthDebit: number;
  byCategory: CategoryTotal[];
}

export interface TrendPoint {
  month: string;
  debit: number;
  credit: number;
}

export interface RuleDto {
  ruleId: number;
  subject: string;
  categoryCode: string;
  categoryName: string;
  ruleText: string;
  hitCount: number;
  hasEmbedding: boolean;
  updatedAt: string;
}

export interface ToolInvocation {
  tool: string;
  arguments: string;
  resultCount: number;
}

export interface AskResponse {
  answer: string;
  toolsUsed: ToolInvocation[];
}

export interface JobAccepted {
  jobId: number;
  count: number;
}

export interface AccountDto {
  accountId: number;
  bankCode: string | null;
  last4: string | null;
  nickname: string | null;
  ibanTail: string | null;
  isOwn: boolean;
  transactionCount: number;
}

export interface UpsertAccountRequest {
  bankCode?: string | null;
  last4?: string | null;
  nickname?: string | null;
  ibanTail?: string | null;
  isOwn: boolean;
}

export interface PatchTransactionRequest {
  categoryCode?: string | null;
  amount?: number | null;
  txnDate?: string | null;
  counterparty?: string | null;
  direction?: Direction | null;
}

export interface LedgerFilters {
  month?: string;
  category?: string;
  direction?: Direction;
  accountId?: number;
  q?: string;
  needsReview?: boolean;
  page?: number;
  pageSize?: number;
}
