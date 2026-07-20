import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  CircleAlert,
  Eye,
  EyeOff,
  KeyRound,
  LoaderCircle,
  LockKeyhole,
  Search,
  ShieldCheck,
  ShieldOff,
  SlidersHorizontal,
} from 'lucide-react'
import type { LucideIcon } from 'lucide-react'
import { m } from 'motion/react'
import { useDeferredValue, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiPermissionGroups,
  getApiPermissionGroupsByIdPageComponents,
  patchApiPermissionComponentMapping,
  type PermissionComponentAccessResDto,
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
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Switch } from '@/components/ui/switch'
import { AccessTabs } from '@/features/access/access-workspace'
import { surfaceEnter } from '@/lib/motion'
import { cn } from '@/lib/utils'

type ModeFilter =
  'all' | 'Configurable' | 'ActionMatrix' | 'Required' | 'OutsideRoleCeiling'

function modeKey(mode?: string | null) {
  switch (mode) {
    case 'Configurable':
      return 'access.permissions.modeConfigurable'
    case 'ActionMatrix':
      return 'access.permissions.modeAction'
    case 'Required':
      return 'access.permissions.modeRequired'
    case 'OutsideRoleCeiling':
      return 'access.permissions.modeOutside'
    default:
      return 'access.permissions.modeUnknown'
  }
}

function modeVariant(mode?: string | null) {
  if (mode === 'Configurable') return 'default' as const
  if (mode === 'ActionMatrix') return 'secondary' as const
  return 'outline' as const
}

export function AccessPermissionsPage() {
  const { t } = useTranslation()
  const { hasPermission, user, refresh } = useAuth()
  const queryClient = useQueryClient()
  const canView = hasPermission(permissions.permissionView)
  const canManage = hasPermission(permissions.permissionManage)
  const [selectedGroupId, setSelectedGroupId] = useState('')
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search.trim().toLowerCase())
  const [mode, setMode] = useState<ModeFilter>('all')
  const [updatingId, setUpdatingId] = useState('')

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
    if (!selectedGroupId && groupsQuery.data?.[0]?.id)
      setSelectedGroupId(groupsQuery.data[0].id)
  }, [groupsQuery.data, selectedGroupId])

  const componentsQuery = useQuery({
    queryKey: ['access', 'permissions', selectedGroupId],
    enabled: canView && Boolean(selectedGroupId),
    queryFn: async () => {
      const result = await getApiPermissionGroupsByIdPageComponents({
        path: { id: selectedGroupId },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const updateMutation = useMutation({
    mutationFn: async ({
      component,
      isVisible,
      isEnable,
    }: {
      component: PermissionComponentAccessResDto
      isVisible: boolean
      isEnable: boolean
    }) => {
      if (!component.groupPageComponentMappingId || !component.groupId)
        throw new Error('Missing permission mapping identifier.')
      setUpdatingId(component.groupPageComponentMappingId)
      const result = await patchApiPermissionComponentMapping({
        body: {
          pageComponentMappingId: component.groupPageComponentMappingId,
          permissionGroupId: component.groupId,
          isVisible,
          isEnable,
        },
      })
      if (!result.data && !result.response?.ok)
        throw toApiRequestError(result.error, result.response)
    },
    onSuccess: async () => {
      toast.success(t('access.feedback.permissionSaved'))
      await queryClient.invalidateQueries({
        queryKey: ['access', 'permissions', selectedGroupId],
      })
      if (selectedGroupId === user?.groupId) await refresh()
    },
    onError: () => toast.error(t('access.feedback.actionFailed')),
    onSettled: () => setUpdatingId(''),
  })

  const pages = useMemo(() => {
    return (componentsQuery.data ?? [])
      .map((page) => ({
        ...page,
        components: (page.components ?? []).filter((component) => {
          if (mode !== 'all' && component.administrationMode !== mode)
            return false
          if (!deferredSearch) return true
          const haystack = [
            page.pageName,
            page.pageCode,
            component.componentName,
            component.componentCode,
            component.description,
          ]
            .filter(Boolean)
            .join(' ')
            .toLowerCase()
          return haystack.includes(deferredSearch)
        }),
      }))
      .filter((page) => page.components.length > 0)
  }, [componentsQuery.data, deferredSearch, mode])

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

  const allComponents = (componentsQuery.data ?? []).flatMap(
    (page) => page.components ?? [],
  )
  const selectedGroup = groupsQuery.data?.find(
    (group) => group.id === selectedGroupId,
  )
  const summary = {
    configurable: allComponents.filter((item) => item.canConfigure).length,
    visible: allComponents.filter((item) => item.isVisible).length,
    enabled: allComponents.filter((item) => item.isEnable).length,
    actions: allComponents.filter((item) => item.isActionGrant).length,
  }

  return (
    <m.div {...surfaceEnter} className="space-y-6">
      <header>
        <p className="text-primary text-sm font-semibold">
          {t('access.eyebrow')}
        </p>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">
          {t('access.permissions.title')}
        </h1>
        <p className="text-muted-foreground mt-2 max-w-3xl text-sm">
          {t('access.permissions.description')}
        </p>
        <AccessTabs active="permissions" />
      </header>

      <section className="border-border bg-card rounded-2xl border p-4">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div className="min-w-0 flex-1">
            <Label>{t('access.permissions.role')}</Label>
            <div className="mt-2 hidden flex-wrap gap-2 md:flex">
              {(groupsQuery.data ?? []).map((group) => (
                <Button
                  key={group.id}
                  variant={group.id === selectedGroupId ? 'default' : 'outline'}
                  onClick={() => setSelectedGroupId(group.id ?? '')}
                  className="rounded-full"
                >
                  {group.groupName}
                </Button>
              ))}
            </div>
            <Select value={selectedGroupId} onValueChange={setSelectedGroupId}>
              <SelectTrigger
                className="mt-2 w-full md:hidden"
                aria-label={t('access.permissions.role')}
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {(groupsQuery.data ?? []).map((group) => (
                  <SelectItem key={group.id} value={group.id ?? ''}>
                    {group.groupName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="grid gap-3 sm:grid-cols-2 xl:w-[38rem]">
            <div>
              <Label htmlFor="permission-search">
                {t('access.permissions.searchLabel')}
              </Label>
              <div className="relative mt-2">
                <Search className="text-muted-foreground pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2" />
                <Input
                  id="permission-search"
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder={t('access.permissions.searchPlaceholder')}
                  className="pl-9"
                />
              </div>
            </div>
            <div>
              <Label>{t('access.permissions.modeFilter')}</Label>
              <Select
                value={mode}
                onValueChange={(value: string) => setMode(value as ModeFilter)}
              >
                <SelectTrigger
                  className="mt-2 w-full"
                  aria-label={t('access.permissions.modeFilter')}
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('access.common.all')}</SelectItem>
                  <SelectItem value="Configurable">
                    {t('access.permissions.modeConfigurable')}
                  </SelectItem>
                  <SelectItem value="ActionMatrix">
                    {t('access.permissions.modeAction')}
                  </SelectItem>
                  <SelectItem value="Required">
                    {t('access.permissions.modeRequired')}
                  </SelectItem>
                  <SelectItem value="OutsideRoleCeiling">
                    {t('access.permissions.modeOutside')}
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </div>

        <div className="mt-5 grid gap-3 border-t pt-4 sm:grid-cols-2 xl:grid-cols-4">
          <SummaryMetric
            icon={SlidersHorizontal}
            value={summary.configurable}
            label={t('access.permissions.configurableCount')}
          />
          <SummaryMetric
            icon={Eye}
            value={summary.visible}
            label={t('access.permissions.visibleCount')}
          />
          <SummaryMetric
            icon={ShieldCheck}
            value={summary.enabled}
            label={t('access.permissions.enabledCount')}
          />
          <SummaryMetric
            icon={KeyRound}
            value={summary.actions}
            label={t('access.permissions.actionCount')}
          />
        </div>
      </section>

      {!canManage && (
        <div className="border-border bg-muted/30 flex items-start gap-3 rounded-xl border p-4 text-sm">
          <LockKeyhole className="text-muted-foreground mt-0.5 size-4" />
          <p>{t('access.permissions.readOnly')}</p>
        </div>
      )}

      {componentsQuery.isLoading || groupsQuery.isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, index) => (
            <Skeleton key={index} className="h-48 w-full rounded-2xl" />
          ))}
        </div>
      ) : componentsQuery.isError || groupsQuery.isError ? (
        <Empty className="border-border min-h-80 border">
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
            onClick={() => void componentsQuery.refetch()}
          >
            {t('access.common.retry')}
          </Button>
        </Empty>
      ) : pages.length === 0 ? (
        <Empty className="border-border min-h-80 border">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <Search />
            </EmptyMedia>
            <EmptyTitle>{t('access.permissions.emptyTitle')}</EmptyTitle>
            <EmptyDescription>
              {t('access.permissions.emptyDescription')}
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <section
          aria-label={t('access.permissions.matrixLabel', {
            role: selectedGroup?.groupName,
          })}
          className="space-y-4"
        >
          {pages.map((page) => (
            <m.article
              layout
              key={page.pageId}
              className="border-border bg-card overflow-hidden rounded-2xl border"
            >
              <header className="flex flex-col gap-2 border-b p-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h2 className="font-semibold">
                    {page.pageName || page.pageCode}
                  </h2>
                  <p className="text-muted-foreground mt-1 text-xs">
                    {page.pageCode}
                  </p>
                </div>
                <Badge variant="outline">
                  {t('access.permissions.componentCount', {
                    count: page.components.length,
                  })}
                </Badge>
              </header>
              <div className="divide-y">
                {page.components.map((component) => {
                  const isUpdating =
                    updatingId === component.groupPageComponentMappingId
                  const isConfigurable =
                    canManage && component.canConfigure && !isUpdating

                  return (
                    <div
                      key={`${page.pageId}-${component.componentId}`}
                      className="grid gap-4 p-4 lg:grid-cols-[minmax(0,1fr)_9rem_8rem] lg:items-center"
                    >
                      <div className="min-w-0">
                        <div className="flex flex-wrap items-center gap-2">
                          <h3 className="font-medium">
                            {component.componentName || component.componentCode}
                          </h3>
                          <Badge
                            variant={modeVariant(component.administrationMode)}
                          >
                            {t(modeKey(component.administrationMode))}
                          </Badge>
                          {isUpdating && (
                            <LoaderCircle className="text-muted-foreground size-4 animate-spin" />
                          )}
                        </div>
                        <p className="text-muted-foreground mt-1 text-xs">
                          {component.componentCode}
                        </p>
                        {component.description && (
                          <p className="text-muted-foreground mt-2 text-sm">
                            {component.description}
                          </p>
                        )}
                      </div>
                      <PermissionSwitch
                        label={t('access.permissions.visible')}
                        icon={component.isVisible ? Eye : EyeOff}
                        checked={Boolean(component.isVisible)}
                        disabled={!isConfigurable}
                        onCheckedChange={(checked) =>
                          updateMutation.mutate({
                            component,
                            isVisible: checked,
                            isEnable: checked
                              ? Boolean(component.isEnable)
                              : false,
                          })
                        }
                      />
                      <PermissionSwitch
                        label={t('access.permissions.enabled')}
                        icon={ShieldCheck}
                        checked={Boolean(component.isEnable)}
                        disabled={!isConfigurable}
                        onCheckedChange={(checked) =>
                          updateMutation.mutate({
                            component,
                            isVisible: checked
                              ? true
                              : Boolean(component.isVisible),
                            isEnable: checked,
                          })
                        }
                      />
                    </div>
                  )
                })}
              </div>
            </m.article>
          ))}
        </section>
      )}
    </m.div>
  )
}

function SummaryMetric({
  icon: Icon,
  value,
  label,
}: {
  icon: LucideIcon
  value: number
  label: string
}) {
  return (
    <div className="flex items-center gap-3">
      <span className="bg-primary/10 text-primary grid size-9 place-items-center rounded-lg">
        <Icon className="size-4" />
      </span>
      <span>
        <strong className="block text-lg font-semibold tabular-nums">
          {value}
        </strong>
        <span className="text-muted-foreground text-xs">{label}</span>
      </span>
    </div>
  )
}

function PermissionSwitch({
  label,
  icon: Icon,
  checked,
  disabled,
  onCheckedChange,
}: {
  label: string
  icon: LucideIcon
  checked: boolean
  disabled: boolean
  onCheckedChange: (checked: boolean) => void
}) {
  return (
    <div
      className={cn(
        'border-border flex items-center justify-between gap-3 rounded-xl border px-3 py-2',
        disabled && 'bg-muted/30',
      )}
    >
      <span className="flex items-center gap-2 text-sm">
        <Icon className="text-muted-foreground size-4" />
        {label}
      </span>
      <Switch
        checked={checked}
        disabled={disabled}
        onCheckedChange={onCheckedChange}
        aria-label={label}
      />
    </div>
  )
}
