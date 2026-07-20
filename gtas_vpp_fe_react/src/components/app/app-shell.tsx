import { LogOut, UserRound } from 'lucide-react'
import { main as MotionMain } from 'motion/react-m'
import type { CSSProperties } from 'react'
import { useTranslation } from 'react-i18next'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'

import { useAuth } from '@/auth/use-auth'
import { BrandMark } from '@/components/app/brand-mark'
import {
  appNavigation,
  isNavigationItemActive,
} from '@/components/app/app-navigation'
import { LanguageControl, ThemeControl } from '@/components/app/header-controls'
import { NotificationCenter } from '@/features/notifications/notification-center'
import { routeEnter } from '@/lib/motion'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarRail,
  SidebarTrigger,
} from '@/components/ui/sidebar'

function getInitials(value?: string | null) {
  const initials = value
    ?.trim()
    .split(/\s+/)
    .slice(-2)
    .map((part) => part[0]?.toUpperCase())
    .join('')
  return initials || 'U'
}

function AppSidebar() {
  const { t } = useTranslation()
  const { user, hasPermission } = useAuth()
  const location = useLocation()
  const visibleNavigation = appNavigation.filter((item) =>
    item.anyPermission.some(hasPermission),
  )

  return (
    <Sidebar collapsible="icon" className="no-print">
      <SidebarHeader className="border-sidebar-border border-b p-3">
        <div className="flex h-10 items-center gap-3 px-1">
          <BrandMark />
          <div className="min-w-0 group-data-[collapsible=icon]:hidden">
            <p className="truncate text-sm font-semibold tracking-tight">
              GTAS VPP
            </p>
            <p className="text-muted-foreground truncate text-[11px]">
              React preview
            </p>
          </div>
        </div>
      </SidebarHeader>

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>{t('navigation.workspace')}</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {visibleNavigation.map((item) => {
                const active = isNavigationItemActive(location.pathname, item)
                return (
                  <SidebarMenuItem key={item.to}>
                    <SidebarMenuButton
                      asChild
                      isActive={active}
                      tooltip={t(item.labelKey)}
                    >
                      <NavLink to={item.to} viewTransition>
                        <item.icon aria-hidden="true" />
                        <span>{t(item.labelKey)}</span>
                      </NavLink>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                )
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter className="border-sidebar-border border-t p-3">
        <div className="flex items-center gap-3 px-1 py-1">
          <Avatar className="size-8">
            <AvatarFallback>{getInitials(user?.fullName)}</AvatarFallback>
          </Avatar>
          <div className="min-w-0 group-data-[collapsible=icon]:hidden">
            <p className="truncate text-xs font-medium">{user?.fullName}</p>
            <p className="text-muted-foreground truncate text-[11px]">
              {user?.primaryDepartmentCode}
            </p>
          </div>
        </div>
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  )
}

export function AppShell() {
  const { t } = useTranslation()
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const activeNavigation = appNavigation.find((item) =>
    isNavigationItemActive(location.pathname, item),
  )

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true, viewTransition: true })
  }

  return (
    <SidebarProvider
      style={
        {
          '--sidebar-width': '14rem',
          '--sidebar-width-icon': '4rem',
        } as CSSProperties
      }
    >
      <a
        href="#main-content"
        className="bg-background text-foreground focus:ring-ring fixed top-2 left-2 z-[100] -translate-y-20 rounded-md px-3 py-2 text-sm shadow-lg focus:translate-y-0 focus:ring-2 focus:outline-none"
      >
        {t('accessibility.skipToContent')}
      </a>
      <AppSidebar />
      <SidebarInset className="min-w-0">
        <header className="no-print bg-background/95 supports-[backdrop-filter]:bg-background/85 sticky top-0 z-30 flex h-16 items-center justify-between border-b px-4 backdrop-blur sm:px-6">
          <div className="flex min-w-0 items-center gap-3">
            <SidebarTrigger className="-ml-1" />
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold">
                {t(activeNavigation?.labelKey ?? 'navigation.workspace')}
              </p>
              <p className="text-muted-foreground hidden truncate text-xs sm:block">
                {user?.primaryDepartmentName || user?.primaryDepartmentCode}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <LanguageControl compact />
            <ThemeControl />
            <NotificationCenter />
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button
                  type="button"
                  className="focus-visible:ring-ring rounded-full focus-visible:ring-2 focus-visible:outline-none"
                  aria-label={t('user.openMenu')}
                >
                  <Avatar className="size-9">
                    <AvatarFallback>
                      {getInitials(user?.fullName)}
                    </AvatarFallback>
                  </Avatar>
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-64 p-2">
                <DropdownMenuLabel className="flex items-start gap-3 px-2 py-2">
                  <UserRound
                    className="text-muted-foreground mt-0.5 size-4"
                    aria-hidden="true"
                  />
                  <span className="min-w-0">
                    <span className="block truncate text-sm text-current">
                      {user?.fullName}
                    </span>
                    <span className="text-muted-foreground block truncate text-xs font-normal">
                      {user?.groupName} · {user?.primaryDepartmentCode}
                    </span>
                  </span>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  variant="destructive"
                  className="px-2 py-2"
                  onSelect={() => void handleLogout()}
                >
                  <LogOut aria-hidden="true" />
                  {t('auth.logout')}
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </header>
        <MotionMain
          key={location.pathname}
          id="main-content"
          tabIndex={-1}
          {...routeEnter}
          className="min-w-0 flex-1"
        >
          <Outlet />
        </MotionMain>
      </SidebarInset>
    </SidebarProvider>
  )
}
