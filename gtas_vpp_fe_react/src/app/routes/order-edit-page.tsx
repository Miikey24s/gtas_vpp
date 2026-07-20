import { useParams } from 'react-router-dom'

import { OrderEditWorkspace } from '@/features/orders/order-edit-workspace'

export function OrderEditPage() {
  const { orderId } = useParams()
  if (!orderId) return null
  return <OrderEditWorkspace orderId={orderId} />
}
