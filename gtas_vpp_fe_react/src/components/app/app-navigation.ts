import {
  BarChart3,
  Building2,
  CalendarRange,
  ClipboardList,
  Landmark,
  LibraryBig,
  PackageSearch,
  Shield,
  ShieldCheck,
  type LucideIcon,
} from 'lucide-react'

import { permissions, type PermissionCode } from '@/auth/permissions'

export type AppNavigationItem = {
  to: string
  labelKey: string
  icon: LucideIcon
  anyPermission: PermissionCode[]
}

export const appNavigation: AppNavigationItem[] = [
  {
    to: '/app/orders',
    labelKey: 'navigation.myOrders',
    icon: ClipboardList,
    anyPermission: [permissions.requestViewOwn],
  },
  {
    to: '/app/catalog',
    labelKey: 'navigation.catalog',
    icon: PackageSearch,
    anyPermission: [permissions.requestCatalogView],
  },
  {
    to: '/app/management/department',
    labelKey: 'navigation.departmentManagement',
    icon: Building2,
    anyPermission: [permissions.requestViewDepartment],
  },
  {
    to: '/app/management/company',
    labelKey: 'navigation.companyManagement',
    icon: Landmark,
    anyPermission: [permissions.requestViewAll],
  },
  {
    to: '/app/management/supplements',
    labelKey: 'navigation.supplementApprovals',
    icon: ShieldCheck,
    anyPermission: [permissions.requestApprove, permissions.requestReject],
  },
  {
    to: '/app/periods',
    labelKey: 'navigation.periods',
    icon: CalendarRange,
    anyPermission: [permissions.periodSettle],
  },
  {
    to: '/app/library',
    labelKey: 'navigation.library',
    icon: LibraryBig,
    anyPermission: [permissions.libraryView],
  },
  {
    to: '/app/access',
    labelKey: 'navigation.access',
    icon: Shield,
    anyPermission: [permissions.permissionView],
  },
  {
    to: '/app/reports',
    labelKey: 'navigation.reports',
    icon: BarChart3,
    anyPermission: [
      permissions.reportViewOwn,
      permissions.reportViewDepartment,
      permissions.reportViewAll,
    ],
  },
]

export function isNavigationItemActive(
  pathname: string,
  item: AppNavigationItem,
) {
  return pathname === item.to || pathname.startsWith(`${item.to}/`)
}
