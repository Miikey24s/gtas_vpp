import { useQuery } from '@tanstack/react-query'
import {
  CircleAlert,
  Eye,
  KeyRound,
  LockKeyhole,
  ShieldCheck,
  ShieldOff,
  UsersRound,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { AnimatePresence, m } from 'motion/react'
import { useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiPermissionGroups,
  getApiPermissionGroupsByIdPageComponents,
  getApiPermissionUsers,
  type PermissionGroupResDto,
} from '@/api/generated'
import { permissions } from '@/auth/permissions'
import { useAuth } from '@/auth/use-auth'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from '@/components/ui/empty'
import { Skeleton } from '@/components/ui/skeleton'
import { AccessTabs } from '@/features/access/access-workspace'
import { surfaceEnter } from '@/lib/motion'
import { cn } from '@/lib/utils'

function totalFromResponse(value: string | null, fallback: number) {
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : fallback
}

function roleAccent(code?: string | null) {
  switch (code) {
    case 'SYSTEM_ADMIN':
      return 'border-violet-500/40 bg-violet-500/5'
    case 'COMPANY_MANAGER':
      return 'border-blue-500/40 bg-blue-500/5'
    case 'DEPARTMENT_MANAGER':
      return 'border-emerald-500/40 bg-emerald-500/5'
    default:
      return 'border-border bg-card'
  }
}

export function AccessGroupsPage() {
  const { t } = useTranslation()
  const { hasPermission } = useAuth()
  const canView = hasPermission(permissions.permissionView)
  const [selectedId, setSelectedId] = useState('')

  const groupsQuery = useQuery({
    queryKey: ['access', 'groups'],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiPermissionGroups({
        query: { getFullName: false, top: 100 },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  useEffect(() => {
    if (!selectedId && groupsQuery.data?.[0]?.id)
      setSelectedId(groupsQuery.data[0].id)
  }, [groupsQuery.data, selectedId])

  const memberCountsQuery = useQuery({
    queryKey: [
      'access',
      'groups',
      'member-counts',
      groupsQuery.data?.map((group) => group.id).join(','),
    ],
    enabled: canView && Boolean(groupsQuery.data?.length),
    queryFn: async () => {
      const counts = await Promise.all(
        (groupsQuery.data ?? []).map(async (group) => {
          if (!group.id) return [group.id, 0] as const
          const result = await getApiPermissionUsers({
            query: { groupId: group.id, hasActiveMembership: true, top: 1 },
          })
          if (!result.data)
            throw toApiRequestError(result.error, result.response)
          return [
            group.id,
            totalFromResponse(
              result.response?.headers.get('X-Total-Count') ?? null,
              result.data.length,
            ),
          ] as const
        }),
      )
      return new Map(counts)
    },
  })

  const componentsQuery = useQuery({
    queryKey: ['access', 'groups', selectedId, 'components'],
    enabled: canView && Boolean(selectedId),
    queryFn: async () => {
      const result = await getApiPermissionGroupsByIdPageComponents({
        path: { id: selectedId },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const selected = useMemo(
    () => groupsQuery.data?.find((group) => group.id === selectedId),
    [groupsQuery.data, selectedId],
  )

  if (!canView) {
    return (
      <Empty className="border-border border">
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <ShieldOff />
          </EmptyMedia>
          <EmptyTitle>{t('access.forbiddenTitle')}</EmptyTitle>
          <EmptyDescription>
            {t('access.forbiddenDescription', {
              permission: permissions.permissionView,
            })}
          </EmptyDescription>
        </EmptyHeader>
      </Empty>
    )
  }

  const pages = componentsQuery.data ?? []
  const components = pages.flatMap((page) => page.components ?? [])
  const actionCount = components.filter((item) => item.isActionGrant).length
  const visibleCount = components.filter((item) => item.isVisible).length
  const memberCount = memberCountsQuery.data?.get(selectedId) ?? 0

  return (
    <m.div {...surfaceEnter} className="space-y-6">
      <header>
        <p className="text-primary text-sm font-semibold">
          {t('access.eyebrow')}
        </p>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">
          {t('access.groups.title')}
        </h1>
        <p className="text-muted-foreground mt-2 max-w-3xl text-sm">
          {t('access.groups.description')}
        </p>
        <AccessTabs active="groups" />
      </header>

      <div className="grid gap-5 xl:grid-cols-[22rem_minmax(0,1fr)]">
        <section
          aria-label={t('access.groups.listLabel')}
          className="space-y-3"
        >
          {groupsQuery.isLoading
            ? Array.from({ length: 4 }).map((_, index) => (
                <Skeleton key={index} className="h-32 w-full rounded-2xl" />
              ))
            : (groupsQuery.data ?? []).map((group) => (
                <RoleCard
                  key={group.id}
                  group={group}
                  selected={group.id === selectedId}
                  memberCount={memberCountsQuery.data?.get(group.id ?? '') ?? 0}
                  onSelect={() => setSelectedId(group.id ?? '')}
                />
              ))}
        </section>

        <section className="min-w-0">
          {groupsQuery.isError || componentsQuery.isError ? (
            <Empty className="border-border min-h-96 border">
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <CircleAlert />
                </EmptyMedia>
                <EmptyTitle>{t('access.common.loadError')}</EmptyTitle>
                <EmptyDescription>
                  {t('access.common.loadErrorDescription')}
                </EmptyDescription>
              </EmptyHeader>
              <Button
                variant="outline"
                onClick={() => {
                  void groupsQuery.refetch()
                  void componentsQuery.refetch()
                }}
              >
                {t('access.common.retry')}
              </Button>
            </Empty>
          ) : !selected || componentsQuery.isLoading ? (
            <div className="space-y-4">
              <Skeleton className="h-48 w-full rounded-2xl" />
              <Skeleton className="h-72 w-full rounded-2xl" />
            </div>
          ) : (
            <AnimatePresence mode="wait">
              <m.div
                key={selected.id}
                initial={{ opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -4 }}
                transition={{ duration: 0.18 }}
                className="space-y-5"
              >
                <div className="border-border bg-card rounded-2xl border p-5">
                  <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                      <Badge variant="outline">{selected.groupCode}</Badge>
                      <h2 className="mt-3 text-2xl font-semibold tracking-tight">
                        {selected.groupName}
                      </h2>
                      <p className="text-muted-foreground mt-2 max-w-2xl text-sm">
                        {selected.description ||
                          t('access.groups.noDescription')}
                      </p>
                    </div>
                    <Badge variant="secondary" className="w-fit">
                      <LockKeyhole className="size-3.5" />
                      {t('access.groups.immutable')}
                    </Badge>
                  </div>
                  <dl className="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                    <Metric
                      icon={UsersRound}
                      value={memberCount}
                      label={t('access.groups.members')}
                    />
                    <Metric
                      icon={ShieldCheck}
                      value={pages.length}
                      label={t('access.groups.pages')}
                    />
                    <Metric
                      icon={Eye}
                      value={visibleCount}
                      label={t('access.groups.visibleComponents')}
                    />
                    <Metric
                      icon={KeyRound}
                      value={actionCount}
                      label={t('access.groups.actionGrants')}
                    />
                  </dl>
                </div>

                <div className="border-border bg-card rounded-2xl border">
                  <div className="border-b p-5">
                    <h2 className="font-semibold">
                      {t('access.groups.capabilityMap')}
                    </h2>
                    <p className="text-muted-foreground mt-1 text-sm">
                      {t('access.groups.capabilityDescription')}
                    </p>
                  </div>
                  <div className="divide-y">
                    {pages.map((page) => (
                      <article key={page.pageId} className="p-5">
                        <div className="flex flex-wrap items-start justify-between gap-3">
                          <div>
                            <h3 className="font-semibold">
                              {page.pageName || page.pageCode}
                            </h3>
                            <p className="text-muted-foreground mt-1 text-xs">
                              {page.pageCode}
                            </p>
                          </div>
                          <Badge variant="outline">
                            {t('access.groups.componentCount', {
                              count: page.components?.length ?? 0,
                            })}
                          </Badge>
                        </div>
                        <div className="mt-4 flex flex-wrap gap-2">
                          {(page.components ?? []).map((component) => (
                            <Badge
                              key={`${page.pageId}-${component.componentId}`}
                              variant={
                                component.isActionGrant
                                  ? 'secondary'
                                  : component.isVisible
                                    ? 'default'
                                    : 'outline'
                              }
                            >
                              {component.componentName ||
                                component.componentCode}
                            </Badge>
                          ))}
                        </div>
                      </article>
                    ))}
                  </div>
                </div>
              </m.div>
            </AnimatePresence>
          )}
        </section>
      </div>
    </m.div>
  )
}

function RoleCard({
  group,
  selected,
  memberCount,
  onSelect,
}: {
  group: PermissionGroupResDto
  selected: boolean
  memberCount: number
  onSelect: () => void
}) {
  const { t } = useTranslation()

  return (
    <button
      type="button"
      onClick={onSelect}
      aria-pressed={selected}
      className={cn(
        'focus-visible:ring-ring w-full rounded-2xl border p-4 text-left transition-[border-color,background-color,transform,box-shadow] duration-200 focus-visible:ring-2 focus-visible:outline-none',
        roleAccent(group.groupCode),
        selected && 'border-primary shadow-sm',
        !selected && 'hover:-translate-y-0.5 hover:shadow-sm',
      )}
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-muted-foreground text-xs font-medium tracking-wide uppercase">
            {group.groupCode}
          </p>
          <h2 className="mt-1 font-semibold">{group.groupName}</h2>
        </div>
        {selected && <ShieldCheck className="text-primary size-5" />}
      </div>
      <p className="text-muted-foreground mt-3 line-clamp-2 text-sm">
        {group.description || t('access.groups.noDescription')}
      </p>
      <p className="mt-4 text-sm font-medium tabular-nums">
        {t('access.groups.memberCount', { count: memberCount })}
      </p>
    </button>
  )
}

function Metric({
  icon: Icon,
  value,
  label,
}: {
  icon: LucideIcon
  value: number
  label: string
}) {
  return (
    <div className="bg-muted/40 rounded-xl p-3">
      <Icon className="text-primary size-4" />
      <dd className="mt-3 text-xl font-semibold tabular-nums">{value}</dd>
      <dt className="text-muted-foreground mt-1 text-xs">{label}</dt>
    </div>
  )
}
