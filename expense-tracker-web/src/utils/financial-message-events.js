export const FINANCIAL_MESSAGES_UPDATED_EVENT = 'financial-messages:updated'

export function notifyFinancialMessagesUpdated(unreadCount) {
  window.dispatchEvent(new CustomEvent(FINANCIAL_MESSAGES_UPDATED_EVENT, {
    detail: { unreadCount },
  }))
}
