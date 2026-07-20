import { createBrowserRouter, Navigate, redirect } from 'react-router-dom'

import { RouteFallback } from '@/app/routes/route-fallback'
import { RouteErrorPage } from '@/app/routes/route-error-page'
import { RequireAuth, RequireSession } from '@/auth/require-auth'
import { AppShell } from '@/components/app/app-shell'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <Navigate to="/login" replace />,
  },
  {
    path: '/login',
    errorElement: <RouteErrorPage />,
    hydrateFallbackElement: <RouteFallback />,
    lazy: async () => {
      const { LoginPage } = await import('@/app/routes/login-page')
      return { Component: LoginPage }
    },
  },
  {
    path: '/register',
    errorElement: <RouteErrorPage />,
    hydrateFallbackElement: <RouteFallback />,
    lazy: async () => {
      const { RegisterPage } = await import('@/app/routes/register-page')
      return { Component: RegisterPage }
    },
  },
  {
    path: '/forgot-password',
    errorElement: <RouteErrorPage />,
    hydrateFallbackElement: <RouteFallback />,
    lazy: async () => {
      const { ForgotPasswordPage } =
        await import('@/app/routes/forgot-password-page')
      return { Component: ForgotPasswordPage }
    },
  },
  {
    path: '/reset-password',
    errorElement: <RouteErrorPage />,
    hydrateFallbackElement: <RouteFallback />,
    lazy: async () => {
      const { ResetPasswordPage } =
        await import('@/app/routes/reset-password-page')
      return { Component: ResetPasswordPage }
    },
  },
  {
    path: '/account/confirm-email',
    errorElement: <RouteErrorPage />,
    hydrateFallbackElement: <RouteFallback />,
    lazy: async () => {
      const { ConfirmEmailPage } =
        await import('@/app/routes/confirm-email-page')
      return { Component: ConfirmEmailPage }
    },
  },
  {
    element: <RequireSession />,
    children: [
      {
        path: '/change-password',
        errorElement: <RouteErrorPage />,
        hydrateFallbackElement: <RouteFallback />,
        lazy: async () => {
          const { ChangePasswordPage } =
            await import('@/app/routes/change-password-page')
          return { Component: ChangePasswordPage }
        },
      },
    ],
  },
  ...[
    ['/Account/Login', '/login'],
    ['/Account/Register', '/register'],
    ['/Account/ForgotPassword', '/forgot-password'],
    ['/Account/ResetPassword', '/reset-password'],
    ['/Account/ConfirmEmail', '/account/confirm-email'],
    ['/Account/ChangePassword', '/change-password'],
  ].map(([path, target]) => ({
    path,
    loader: ({ request }: { request: Request }) => {
      const source = new URL(request.url)
      return redirect(`${target}${source.search}`)
    },
  })),
  {
    element: <RequireAuth />,
    children: [
      {
        path: '/app',
        element: <AppShell />,
        errorElement: <RouteErrorPage />,
        children: [
          { index: true, loader: () => redirect('/app/orders') },
          {
            path: 'orders',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { MyOrdersPage } =
                await import('@/app/routes/my-orders-page')
              return { Component: MyOrdersPage }
            },
          },
          {
            path: 'orders/new',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { OrderCreatePage } =
                await import('@/app/routes/order-create-page')
              return { Component: OrderCreatePage }
            },
          },
          {
            path: 'orders/:orderId/history',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { OrderHistoryPage } =
                await import('@/app/routes/order-history-page')
              return { Component: OrderHistoryPage }
            },
          },
          {
            path: 'orders/:orderId/edit',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { OrderEditPage } =
                await import('@/app/routes/order-edit-page')
              return { Component: OrderEditPage }
            },
          },
          {
            path: 'orders/:orderId',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { OrderDetailPage } =
                await import('@/app/routes/order-detail-page')
              return { Component: OrderDetailPage }
            },
          },
          {
            path: 'catalog',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { CatalogPage } = await import('@/app/routes/catalog-page')
              return { Component: CatalogPage }
            },
          },
          {
            path: 'management/department',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { DepartmentManagementPage } =
                await import('@/app/routes/department-management-page')
              return { Component: DepartmentManagementPage }
            },
          },
          {
            path: 'management/company',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { CompanyManagementPage } =
                await import('@/app/routes/company-management-page')
              return { Component: CompanyManagementPage }
            },
          },
          {
            path: 'management/supplements',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { SupplementApprovalsPage } =
                await import('@/app/routes/supplement-approvals-page')
              return { Component: SupplementApprovalsPage }
            },
          },
          {
            path: 'periods',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { PeriodsPage } = await import('@/app/routes/periods-page')
              return { Component: PeriodsPage }
            },
          },
          {
            path: 'periods/:year/:month',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { PeriodOverviewPage } =
                await import('@/app/routes/period-overview-page')
              return { Component: PeriodOverviewPage }
            },
          },
          {
            path: 'periods/:year/:month/preview',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { SettlementPreviewPage } =
                await import('@/app/routes/settlement-preview-page')
              return { Component: SettlementPreviewPage }
            },
          },
          {
            path: 'periods/:year/:month/settlement',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { SettlementDetailPage } =
                await import('@/app/routes/settlement-detail-page')
              return { Component: SettlementDetailPage }
            },
          },
          {
            path: 'library',
            loader: () => redirect('/app/library/classes'),
          },
          {
            path: 'library/classes',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { LibraryClassesPage } =
                await import('@/app/routes/library-classes-page')
              return { Component: LibraryClassesPage }
            },
          },
          {
            path: 'library/categories',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { LibraryCategoriesPage } =
                await import('@/app/routes/library-categories-page')
              return { Component: LibraryCategoriesPage }
            },
          },
          {
            path: 'library/items',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { LibraryItemsPage } =
                await import('@/app/routes/library-items-page')
              return { Component: LibraryItemsPage }
            },
          },
          {
            path: 'library/suppliers',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { LibrarySuppliersPage } =
                await import('@/app/routes/library-suppliers-page')
              return { Component: LibrarySuppliersPage }
            },
          },
          {
            path: 'library/departments',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { LibraryDepartmentsPage } =
                await import('@/app/routes/library-departments-page')
              return { Component: LibraryDepartmentsPage }
            },
          },
          {
            path: 'library/price-lists',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { LibraryPriceListsPage } =
                await import('@/app/routes/library-price-lists-page')
              return { Component: LibraryPriceListsPage }
            },
          },
          {
            path: 'library/prices',
            hydrateFallbackElement: <RouteFallback />,
            lazy: async () => {
              const { LibraryPricesPage } =
                await import('@/app/routes/library-prices-page')
              return { Component: LibraryPricesPage }
            },
          },
        ],
      },
    ],
  },
  {
    path: '*',
    hydrateFallbackElement: <RouteFallback />,
    lazy: async () => {
      const { NotFoundPage } = await import('@/app/routes/not-found-page')
      return { Component: NotFoundPage }
    },
  },
])
