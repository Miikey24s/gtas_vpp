export const permissions = {
  requestViewOwn: 'REQUEST_VIEW_OWN',
  requestViewDepartment: 'REQUEST_VIEW_DEPARTMENT',
  requestViewAll: 'REQUEST_VIEW_ALL',
  requestCreate: 'REQUEST_CREATE',
  requestUpdateOwn: 'REQUEST_UPDATE_OWN',
  requestCancelOwn: 'REQUEST_CANCEL_OWN',
  requestApprove: 'REQUEST_APPROVE',
  requestReject: 'REQUEST_REJECT',
  requestCatalogView: 'REQUEST_CATALOG_VIEW',
  libraryView: 'LIBRARY_VIEW',
  libraryManage: 'LIBRARY_MANAGE',
  permissionView: 'PERMISSION_VIEW',
  permissionManage: 'PERMISSION_MANAGE',
  reportViewOwn: 'REPORT_VIEW_OWN',
  reportViewDepartment: 'REPORT_VIEW_DEPARTMENT',
  reportViewAll: 'REPORT_VIEW_ALL',
  reportExport: 'REPORT_EXPORT',
  periodSettle: 'PERIOD_SETTLE',
} as const

export type PermissionCode = (typeof permissions)[keyof typeof permissions]
