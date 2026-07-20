import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ChevronLeft,
  ChevronRight,
  CircleAlert,
  Copy,
  KeyRound,
  MoreHorizontal,
  RefreshCw,
  Search,
  ShieldCheck,
  ShieldOff,
  UserCheck,
  UserCog,
  UserRoundX,
  UsersRound,
} from 'lucide-react'
import { m } from 'motion/react'
import { useDeferredValue, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'

import { toApiRequestError } from '@/api/api-error'
import {
  getApiLibraryByTableCode,
  getApiPermissionGroups,
  getApiPermissionUsers,
  postApiAccountAdminActivate,
  postApiAccountAdminResetPassword,
  postApiPermissionMembershipsDeactivate,
  putApiPermissionMemberships,
  type UserAdministrationResDto,
} from '@/api/generated'
import { permissions } from '@/auth/permissions'
import { useAuth } from '@/auth/use-auth'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
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
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Textarea } from '@/components/ui/textarea'
import { AccessTabs } from '@/features/access/access-workspace'
import { surfaceEnter } from '@/lib/motion'
import { cn } from '@/lib/utils'

type AccountFilter = 'all' | 'pending' | 'active' | 'unassigned' | 'disabled'

type MembershipForm = {
  groupId: string
  departmentId: string
  reason: string
}

type DepartmentOption = {
  id?: string
  name?: string | null
}

const pageSize = 20
const emptyGuid = '00000000-0000-0000-0000-000000000000'

function accountQuery(filter: AccountFilter) {
  switch (filter) {
    case 'pending':
      return { accountStatus: 'PendingApproval' }
    case 'active':
      return { accountStatus: 'Active', hasActiveMembership: true }
    case 'unassigned':
      return { accountStatus: 'Active', hasActiveMembership: false }
    case 'disabled':
      return { accountStatus: 'Disabled' }
    default:
      return {}
  }
}

function countFromHeader(value: string | null, fallback: number) {
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : fallback
}

function generateTemporaryPassword() {
  const bytes = new Uint8Array(8)
  window.crypto.getRandomValues(bytes)
  return `Tmp-${Array.from(bytes, (value) => value.toString(16).padStart(2, '0')).join('')}aA1!`
}

function statusKey(user: UserAdministrationResDto) {
  if (user.accountStatus === 'PendingApproval') return 'access.status.pending'
  if (user.accountStatus === 'Disabled') return 'access.status.disabled'
  if (user.accountStatus === 'Active' && user.isActive)
    return 'access.status.active'
  if (user.accountStatus === 'Active') return 'access.status.unassigned'
  return 'access.status.unknown'
}

function statusVariant(user: UserAdministrationResDto) {
  if (user.accountStatus === 'PendingApproval') return 'secondary' as const
  if (user.accountStatus === 'Disabled') return 'destructive' as const
  if (user.accountStatus === 'Active' && user.isActive)
    return 'default' as const
  return 'outline' as const
}

export function AccessUsersPage() {
  const { t } = useTranslation()
  const { hasPermission, user: currentUser } = useAuth()
  const queryClient = useQueryClient()
  const canView = hasPermission(permissions.permissionView)
  const canManage = hasPermission(permissions.permissionManage)
  const [search, setSearch] = useState('')
  const deferredSearch = useDeferredValue(search.trim())
  const [filter, setFilter] = useState<AccountFilter>('all')
  const [groupId, setGroupId] = useState('all')
  const [departmentId, setDepartmentId] = useState('all')
  const [page, setPage] = useState(0)
  const [editorTarget, setEditorTarget] =
    useState<UserAdministrationResDto | null>(null)
  const [membershipForm, setMembershipForm] = useState<MembershipForm>({
    groupId: '',
    departmentId: '',
    reason: '',
  })
  const [deactivateTarget, setDeactivateTarget] =
    useState<UserAdministrationResDto | null>(null)
  const [resetTarget, setResetTarget] =
    useState<UserAdministrationResDto | null>(null)
  const [temporaryPassword, setTemporaryPassword] = useState('')

  useEffect(() => setPage(0), [deferredSearch, filter, groupId, departmentId])

  const groupsQuery = useQuery({
    queryKey: ['access', 'groups', 'lookup'],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiPermissionGroups({
        query: { getFullName: false, top: 100 },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  const departmentsQuery = useQuery({
    queryKey: ['access', 'departments', 'lookup'],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiLibraryByTableCode({
        path: { tableCode: 'departments' },
        query: { top: 1000, showDeleted: false, orderby: 'Name asc' },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data as DepartmentOption[]
    },
  })

  const summaryQuery = useQuery({
    queryKey: ['access', 'users', 'summary'],
    enabled: canView,
    queryFn: async () => {
      const requests = (
        ['pending', 'active', 'unassigned', 'disabled'] as const
      ).map(async (status) => {
        const result = await getApiPermissionUsers({
          query: { ...accountQuery(status), skip: 0, top: 1 },
        })
        if (!result.data) throw toApiRequestError(result.error, result.response)
        return [
          status,
          countFromHeader(
            result.response?.headers.get('X-Total-Count') ?? null,
            result.data.length,
          ),
        ] as const
      })
      return Object.fromEntries(await Promise.all(requests)) as Record<
        Exclude<AccountFilter, 'all'>,
        number
      >
    },
  })

  const usersQuery = useQuery({
    queryKey: [
      'access',
      'users',
      deferredSearch,
      filter,
      groupId,
      departmentId,
      page,
    ],
    enabled: canView,
    queryFn: async () => {
      const result = await getApiPermissionUsers({
        query: {
          search: deferredSearch || undefined,
          ...accountQuery(filter),
          groupId: groupId === 'all' ? undefined : groupId,
          departmentId: departmentId === 'all' ? undefined : departmentId,
          skip: page * pageSize,
          top: pageSize,
        },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return {
        items: result.data,
        total: countFromHeader(
          result.response?.headers.get('X-Total-Count') ?? null,
          result.data.length,
        ),
      }
    },
  })

  const invalidateUsers = async () => {
    await queryClient.invalidateQueries({ queryKey: ['access', 'users'] })
  }

  const membershipMutation = useMutation({
    mutationFn: async () => {
      if (!editorTarget?.userId) throw new Error('Missing account id.')
      if (!membershipForm.groupId || !membershipForm.departmentId)
        throw new Error(t('access.validation.membershipRequired'))

      const isActivation = editorTarget.accountStatus === 'PendingApproval'
      const result = isActivation
        ? await postApiAccountAdminActivate({
            body: {
              accountId: editorTarget.userId,
              groupId: membershipForm.groupId,
              primaryDepartmentId: membershipForm.departmentId,
              reason: membershipForm.reason || undefined,
            },
          })
        : await putApiPermissionMemberships({
            body: {
              accountId: editorTarget.userId,
              groupId: membershipForm.groupId,
              primaryDepartmentId: membershipForm.departmentId,
              expectedRowVersion: editorTarget.isActive
                ? editorTarget.rowVersion
                : undefined,
              reason: membershipForm.reason || undefined,
            },
          })
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return isActivation
    },
    onSuccess: async (isActivation) => {
      toast.success(
        t(
          isActivation
            ? 'access.feedback.activated'
            : 'access.feedback.membershipSaved',
        ),
      )
      setEditorTarget(null)
      await invalidateUsers()
    },
    onError: () => toast.error(t('access.feedback.actionFailed')),
  })

  const deactivateMutation = useMutation({
    mutationFn: async () => {
      if (!deactivateTarget?.userId || !deactivateTarget.rowVersion)
        throw new Error('Missing membership revision.')
      const result = await postApiPermissionMembershipsDeactivate({
        body: {
          accountId: deactivateTarget.userId,
          expectedRowVersion: deactivateTarget.rowVersion,
          reason: t('access.reasons.deactivate'),
        },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
    },
    onSuccess: async () => {
      toast.success(t('access.feedback.deactivated'))
      setDeactivateTarget(null)
      await invalidateUsers()
    },
    onError: () => toast.error(t('access.feedback.actionFailed')),
  })

  const resetMutation = useMutation({
    mutationFn: async () => {
      if (!resetTarget?.userId || !temporaryPassword)
        throw new Error('Missing reset data.')
      const result = await postApiAccountAdminResetPassword({
        body: {
          accountId: resetTarget.userId,
          temporaryPassword,
          confirmPassword: temporaryPassword,
          reason: t('access.reasons.passwordReset'),
        },
      })
      if (!result.data) throw toApiRequestError(result.error, result.response)
    },
    onSuccess: () => {
      toast.success(t('access.feedback.passwordReset'))
      setResetTarget(null)
      setTemporaryPassword('')
    },
    onError: () => toast.error(t('access.feedback.actionFailed')),
  })

  const groupLookup = useMemo(
    () =>
      new Map(
        (groupsQuery.data ?? []).map((group) => [group.id, group.groupName]),
      ),
    [groupsQuery.data],
  )
  const departmentLookup = useMemo(
    () =>
      new Map(
        (departmentsQuery.data ?? []).map((department) => [
          department.id,
          department.name,
        ]),
      ),
    [departmentsQuery.data],
  )

  const openMembershipEditor = (target: UserAdministrationResDto) => {
    setEditorTarget(target)
    setMembershipForm({
      groupId:
        target.groupId && target.groupId !== emptyGuid ? target.groupId : '',
      departmentId: target.departmentId ?? '',
      reason: '',
    })
  }

  const canEdit = (target: UserAdministrationResDto) =>
    canManage &&
    target.userId !== currentUser?.userId &&
    (target.accountStatus === 'PendingApproval' ||
      target.accountStatus === 'Active')

  const canDeactivate = (target: UserAdministrationResDto) =>
    canEdit(target) &&
    target.accountStatus === 'Active' &&
    Boolean(target.isActive)

  const canReset = (target: UserAdministrationResDto) =>
    canManage &&
    target.userId !== currentUser?.userId &&
    target.accountStatus === 'Active'

  const openReset = (target: UserAdministrationResDto) => {
    setResetTarget(target)
    setTemporaryPassword(generateTemporaryPassword())
  }

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

  const items = usersQuery.data?.items ?? []
  const total = usersQuery.data?.total ?? 0
  const pageCount = Math.max(1, Math.ceil(total / pageSize))
  const summary = summaryQuery.data
  const summaryCards = [
    ['pending', summary?.pending ?? 0, UserCheck],
    ['active', summary?.active ?? 0, ShieldCheck],
    ['unassigned', summary?.unassigned ?? 0, UserCog],
    ['disabled', summary?.disabled ?? 0, UserRoundX],
  ] as const

  return (
    <m.div {...surfaceEnter} className="space-y-6">
      <header>
        <p className="text-primary text-sm font-semibold">
          {t('access.eyebrow')}
        </p>
        <h1 className="mt-2 text-3xl font-semibold tracking-tight">
          {t('access.users.title')}
        </h1>
        <p className="text-muted-foreground mt-2 max-w-3xl text-sm">
          {t('access.users.description')}
        </p>
        <AccessTabs active="users" />
      </header>

      <section
        aria-label={t('access.users.summaryLabel')}
        className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4"
      >
        {summaryCards.map(([key, value, Icon]) => (
          <button
            key={key}
            type="button"
            aria-pressed={filter === key}
            onClick={() => {
              setFilter(key)
              setSearch('')
              setGroupId('all')
              setDepartmentId('all')
            }}
            className={cn(
              'border-border bg-card hover:bg-muted/40 focus-visible:ring-ring flex min-h-24 items-center gap-4 rounded-2xl border p-4 text-left transition-[border-color,background-color,box-shadow] focus-visible:ring-2 focus-visible:outline-none',
              filter === key && 'border-primary bg-primary/5 shadow-sm',
            )}
          >
            <span className="bg-primary/10 text-primary grid size-10 place-items-center rounded-xl">
              <Icon className="size-5" />
            </span>
            <span>
              <strong className="block text-2xl font-semibold tabular-nums">
                {summaryQuery.isLoading ? '—' : value}
              </strong>
              <span className="text-muted-foreground text-sm">
                {t(`access.status.${key}`)}
              </span>
            </span>
          </button>
        ))}
      </section>

      <section className="border-border bg-card rounded-2xl border">
        <div className="flex flex-col gap-3 border-b p-4 xl:flex-row xl:items-end">
          <div className="min-w-0 flex-1">
            <Label htmlFor="access-user-search">
              {t('access.users.searchLabel')}
            </Label>
            <div className="relative mt-2">
              <Search className="text-muted-foreground pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2" />
              <Input
                id="access-user-search"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder={t('access.users.searchPlaceholder')}
                className="pl-9"
              />
            </div>
          </div>
          <div className="grid gap-3 sm:grid-cols-3 xl:w-[46rem]">
            <div>
              <Label>{t('access.users.statusFilter')}</Label>
              <Select
                value={filter}
                onValueChange={(value: string) =>
                  setFilter(value as AccountFilter)
                }
              >
                <SelectTrigger
                  className="mt-2 w-full"
                  aria-label={t('access.users.statusFilter')}
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {(
                    [
                      'all',
                      'pending',
                      'active',
                      'unassigned',
                      'disabled',
                    ] as const
                  ).map((value) => (
                    <SelectItem key={value} value={value}>
                      {t(`access.status.${value}`)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label>{t('access.users.groupFilter')}</Label>
              <Select value={groupId} onValueChange={setGroupId}>
                <SelectTrigger
                  className="mt-2 w-full"
                  aria-label={t('access.users.groupFilter')}
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('access.common.all')}</SelectItem>
                  {(groupsQuery.data ?? []).map((group) => (
                    <SelectItem key={group.id} value={group.id ?? ''}>
                      {group.groupName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label>{t('access.users.departmentFilter')}</Label>
              <Select value={departmentId} onValueChange={setDepartmentId}>
                <SelectTrigger
                  className="mt-2 w-full"
                  aria-label={t('access.users.departmentFilter')}
                >
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">{t('access.common.all')}</SelectItem>
                  {(departmentsQuery.data ?? []).map((department) => (
                    <SelectItem key={department.id} value={department.id ?? ''}>
                      {department.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
          <Button
            variant="outline"
            size="icon"
            onClick={() => void usersQuery.refetch()}
            aria-label={t('access.common.refresh')}
          >
            <RefreshCw className="size-4" />
          </Button>
        </div>

        {usersQuery.isLoading ? (
          <div className="space-y-3 p-4">
            {Array.from({ length: 5 }).map((_, index) => (
              <Skeleton key={index} className="h-14 w-full" />
            ))}
          </div>
        ) : usersQuery.isError ? (
          <Empty className="min-h-72">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <CircleAlert />
              </EmptyMedia>
              <EmptyTitle>{t('access.common.loadError')}</EmptyTitle>
              <EmptyDescription>
                {t('access.common.loadErrorDescription')}
              </EmptyDescription>
            </EmptyHeader>
            <Button variant="outline" onClick={() => void usersQuery.refetch()}>
              {t('access.common.retry')}
            </Button>
          </Empty>
        ) : items.length === 0 ? (
          <Empty className="min-h-72">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <UsersRound />
              </EmptyMedia>
              <EmptyTitle>{t('access.users.emptyTitle')}</EmptyTitle>
              <EmptyDescription>
                {t('access.users.emptyDescription')}
              </EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <>
            <div className="hidden overflow-x-auto lg:block">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('access.users.account')}</TableHead>
                    <TableHead>{t('access.users.status')}</TableHead>
                    <TableHead>{t('access.users.group')}</TableHead>
                    <TableHead>{t('access.users.department')}</TableHead>
                    <TableHead>{t('access.users.session')}</TableHead>
                    <TableHead className="w-16 text-right">
                      {t('access.common.actions')}
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.userId}>
                      <TableCell>
                        <div className="font-medium">
                          {item.fullName || item.userLogin}
                        </div>
                        <div className="text-muted-foreground text-xs">
                          {item.userLogin} · {item.email || '—'}
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={statusVariant(item)}>
                          {t(statusKey(item))}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        {item.groupName ||
                          groupLookup.get(item.groupId ?? '') ||
                          '—'}
                      </TableCell>
                      <TableCell>
                        {item.departmentName ||
                          departmentLookup.get(item.departmentId ?? '') ||
                          '—'}
                      </TableCell>
                      <TableCell className="tabular-nums">
                        v{item.sessionVersion ?? 0}
                      </TableCell>
                      <TableCell className="text-right">
                        <UserActions
                          item={item}
                          canEdit={canEdit(item)}
                          canDeactivate={canDeactivate(item)}
                          canReset={canReset(item)}
                          onEdit={() => openMembershipEditor(item)}
                          onDeactivate={() => setDeactivateTarget(item)}
                          onReset={() => openReset(item)}
                        />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>

            <div className="grid gap-3 p-4 lg:hidden">
              {items.map((item) => (
                <article
                  key={item.userId}
                  className="border-border rounded-xl border p-4"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <h2 className="font-semibold">
                        {item.fullName || item.userLogin}
                      </h2>
                      <p className="text-muted-foreground text-xs">
                        {item.userLogin} · {item.email || '—'}
                      </p>
                    </div>
                    <UserActions
                      item={item}
                      canEdit={canEdit(item)}
                      canDeactivate={canDeactivate(item)}
                      canReset={canReset(item)}
                      onEdit={() => openMembershipEditor(item)}
                      onDeactivate={() => setDeactivateTarget(item)}
                      onReset={() => openReset(item)}
                    />
                  </div>
                  <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
                    <div>
                      <dt className="text-muted-foreground">
                        {t('access.users.status')}
                      </dt>
                      <dd className="mt-1">
                        <Badge variant={statusVariant(item)}>
                          {t(statusKey(item))}
                        </Badge>
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted-foreground">
                        {t('access.users.group')}
                      </dt>
                      <dd className="mt-1 font-medium">
                        {item.groupName || '—'}
                      </dd>
                    </div>
                    <div className="col-span-2">
                      <dt className="text-muted-foreground">
                        {t('access.users.department')}
                      </dt>
                      <dd className="mt-1 font-medium">
                        {item.departmentName || '—'}
                      </dd>
                    </div>
                  </dl>
                </article>
              ))}
            </div>
          </>
        )}

        <div className="flex items-center justify-between gap-3 border-t p-4">
          <p className="text-muted-foreground text-sm">
            {t('access.common.pageSummary', {
              from: total === 0 ? 0 : page * pageSize + 1,
              to: Math.min((page + 1) * pageSize, total),
              total,
            })}
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="icon"
              disabled={page === 0}
              onClick={() => setPage((value) => Math.max(0, value - 1))}
              aria-label={t('access.common.previousPage')}
            >
              <ChevronLeft className="size-4" />
            </Button>
            <span className="min-w-20 text-center text-sm tabular-nums">
              {page + 1} / {pageCount}
            </span>
            <Button
              variant="outline"
              size="icon"
              disabled={page + 1 >= pageCount}
              onClick={() => setPage((value) => value + 1)}
              aria-label={t('access.common.nextPage')}
            >
              <ChevronRight className="size-4" />
            </Button>
          </div>
        </div>
      </section>

      <Sheet
        open={Boolean(editorTarget)}
        onOpenChange={(open: boolean) => !open && setEditorTarget(null)}
      >
        <SheetContent className="overflow-y-auto sm:max-w-lg">
          <SheetHeader>
            <SheetTitle>
              {t(
                editorTarget?.accountStatus === 'PendingApproval'
                  ? 'access.users.activateTitle'
                  : 'access.users.membershipTitle',
              )}
            </SheetTitle>
            <SheetDescription>
              {editorTarget?.fullName || editorTarget?.userLogin}
            </SheetDescription>
          </SheetHeader>
          <div className="grid gap-5 px-4">
            <div>
              <Label>{t('access.users.group')}</Label>
              <Select
                value={membershipForm.groupId}
                onValueChange={(value: string) =>
                  setMembershipForm((current) => ({
                    ...current,
                    groupId: value,
                  }))
                }
              >
                <SelectTrigger
                  className="mt-2 w-full"
                  aria-label={t('access.users.group')}
                >
                  <SelectValue placeholder={t('access.users.selectGroup')} />
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
            <div>
              <Label>{t('access.users.department')}</Label>
              <Select
                value={membershipForm.departmentId}
                onValueChange={(value: string) =>
                  setMembershipForm((current) => ({
                    ...current,
                    departmentId: value,
                  }))
                }
              >
                <SelectTrigger
                  className="mt-2 w-full"
                  aria-label={t('access.users.department')}
                >
                  <SelectValue
                    placeholder={t('access.users.selectDepartment')}
                  />
                </SelectTrigger>
                <SelectContent>
                  {(departmentsQuery.data ?? []).map((department) => (
                    <SelectItem key={department.id} value={department.id ?? ''}>
                      {department.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label htmlFor="membership-reason">
                {t('access.users.reason')}
              </Label>
              <Textarea
                id="membership-reason"
                value={membershipForm.reason}
                onChange={(event) =>
                  setMembershipForm((current) => ({
                    ...current,
                    reason: event.target.value,
                  }))
                }
                placeholder={t('access.users.reasonPlaceholder')}
                className="mt-2"
              />
            </div>
          </div>
          <SheetFooter>
            <Button
              variant="outline"
              onClick={() => setEditorTarget(null)}
              disabled={membershipMutation.isPending}
            >
              {t('access.common.cancel')}
            </Button>
            <Button
              onClick={() => membershipMutation.mutate()}
              disabled={
                membershipMutation.isPending ||
                !membershipForm.groupId ||
                !membershipForm.departmentId
              }
            >
              {t(
                editorTarget?.accountStatus === 'PendingApproval'
                  ? 'access.users.activate'
                  : 'access.common.save',
              )}
            </Button>
          </SheetFooter>
        </SheetContent>
      </Sheet>

      <Dialog
        open={Boolean(deactivateTarget)}
        onOpenChange={(open: boolean) => !open && setDeactivateTarget(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('access.users.deactivateTitle')}</DialogTitle>
            <DialogDescription>
              {t('access.users.deactivateDescription', {
                name: deactivateTarget?.fullName || deactivateTarget?.userLogin,
              })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setDeactivateTarget(null)}
              disabled={deactivateMutation.isPending}
            >
              {t('access.common.cancel')}
            </Button>
            <Button
              variant="destructive"
              onClick={() => deactivateMutation.mutate()}
              disabled={deactivateMutation.isPending}
            >
              {t('access.users.deactivate')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={Boolean(resetTarget)}
        onOpenChange={(open: boolean) => {
          if (!open) {
            setResetTarget(null)
            setTemporaryPassword('')
          }
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('access.users.resetTitle')}</DialogTitle>
            <DialogDescription>
              {t('access.users.resetDescription', {
                name: resetTarget?.fullName || resetTarget?.userLogin,
              })}
            </DialogDescription>
          </DialogHeader>
          <div>
            <Label htmlFor="temporary-password">
              {t('access.users.temporaryPassword')}
            </Label>
            <div className="mt-2 flex gap-2">
              <Input
                id="temporary-password"
                value={temporaryPassword}
                readOnly
                className="font-mono"
              />
              <Button
                variant="outline"
                size="icon"
                onClick={() => {
                  void navigator.clipboard.writeText(temporaryPassword)
                  toast.success(t('access.feedback.copied'))
                }}
                aria-label={t('access.users.copyPassword')}
              >
                <Copy className="size-4" />
              </Button>
            </div>
            <p className="text-muted-foreground mt-2 text-xs">
              {t('access.users.temporaryPasswordHint')}
            </p>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setResetTarget(null)}
              disabled={resetMutation.isPending}
            >
              {t('access.common.cancel')}
            </Button>
            <Button
              onClick={() => resetMutation.mutate()}
              disabled={resetMutation.isPending}
            >
              {t('access.users.confirmReset')}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </m.div>
  )
}

function UserActions({
  item,
  canEdit,
  canDeactivate,
  canReset,
  onEdit,
  onDeactivate,
  onReset,
}: {
  item: UserAdministrationResDto
  canEdit: boolean
  canDeactivate: boolean
  canReset: boolean
  onEdit: () => void
  onDeactivate: () => void
  onReset: () => void
}) {
  const { t } = useTranslation()

  if (!canEdit && !canDeactivate && !canReset) return <span aria-hidden>—</span>

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="icon"
          aria-label={t('access.users.openActions', {
            name: item.fullName || item.userLogin,
          })}
        >
          <MoreHorizontal className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {canEdit && (
          <DropdownMenuItem onClick={onEdit}>
            <UserCog className="size-4" />
            {t(
              item.accountStatus === 'PendingApproval'
                ? 'access.users.activate'
                : item.isActive
                  ? 'access.users.editMembership'
                  : 'access.users.assignMembership',
            )}
          </DropdownMenuItem>
        )}
        {canReset && (
          <DropdownMenuItem onClick={onReset}>
            <KeyRound className="size-4" />
            {t('access.users.resetPassword')}
          </DropdownMenuItem>
        )}
        {canDeactivate && <DropdownMenuSeparator />}
        {canDeactivate && (
          <DropdownMenuItem variant="destructive" onClick={onDeactivate}>
            <ShieldOff className="size-4" />
            {t('access.users.deactivate')}
          </DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
