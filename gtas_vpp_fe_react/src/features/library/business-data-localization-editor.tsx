import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Languages, LoaderCircle, Trash2 } from 'lucide-react'
import { useEffect, useState, type Dispatch, type SetStateAction } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'

import { toApiRequestError } from '@/api/api-error'
import {
  deleteApiBusinessDataByEntityTypeByEntityIdLocalizationTranslationsByLanguageCode,
  getApiBusinessDataByEntityTypeByEntityIdLocalization,
  putApiBusinessDataByEntityTypeByEntityIdLocalizationOriginalLanguage,
  putApiBusinessDataByEntityTypeByEntityIdLocalizationTranslationsByLanguageCode,
  type BusinessDataTranslationBundleResDto,
} from '@/api/generated'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
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
import { Textarea } from '@/components/ui/textarea'

type SupportedLanguage = 'vi' | 'en'

type TranslationDraft = {
  name: string
  description: string
  status: 'Draft' | 'Approved'
}

const emptyDraft: TranslationDraft = {
  name: '',
  description: '',
  status: 'Draft',
}

export function BusinessDataLocalizationEditor({
  entityType,
  entityId,
  onChanged,
}: {
  entityType: string
  entityId: string
  onChanged?: () => void | Promise<void>
}) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [originalLanguage, setOriginalLanguage] =
    useState<SupportedLanguage>('vi')
  const [drafts, setDrafts] = useState<
    Record<SupportedLanguage, TranslationDraft>
  >({
    vi: { ...emptyDraft },
    en: { ...emptyDraft },
  })

  const queryKey = ['business-data-localization', entityType, entityId] as const
  const localizationQuery = useQuery({
    queryKey,
    queryFn: async () => {
      const result = await getApiBusinessDataByEntityTypeByEntityIdLocalization(
        {
          path: { entityType, entityId },
        },
      )
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
  })

  useEffect(() => {
    const data = localizationQuery.data
    if (!data) return
    setOriginalLanguage(data.originalLanguageCode === 'en' ? 'en' : 'vi')
    setDrafts({
      vi: toDraft(data, 'vi'),
      en: toDraft(data, 'en'),
    })
  }, [localizationQuery.data])

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey })
    await onChanged?.()
  }

  const originalLanguageMutation = useMutation({
    mutationFn: async (languageCode: SupportedLanguage) => {
      const result =
        await putApiBusinessDataByEntityTypeByEntityIdLocalizationOriginalLanguage(
          {
            path: { entityType, entityId },
            body: { languageCode },
          },
        )
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      await refresh()
      toast.success(t('library.localization.originalLanguageSaved'))
    },
    onError: () => toast.error(t('library.localization.saveError')),
  })

  const saveTranslationMutation = useMutation({
    mutationFn: async (languageCode: SupportedLanguage) => {
      const draft = drafts[languageCode]
      const result =
        await putApiBusinessDataByEntityTypeByEntityIdLocalizationTranslationsByLanguageCode(
          {
            path: { entityType, entityId, languageCode },
            body: {
              name: draft.name.trim(),
              description: draft.description.trim() || null,
              status: draft.status,
              source: 'Manual',
            },
          },
        )
      if (!result.data) throw toApiRequestError(result.error, result.response)
      return result.data
    },
    onSuccess: async () => {
      await refresh()
      toast.success(t('library.localization.translationSaved'))
    },
    onError: () => toast.error(t('library.localization.saveError')),
  })

  const deleteTranslationMutation = useMutation({
    mutationFn: async (languageCode: SupportedLanguage) => {
      const result =
        await deleteApiBusinessDataByEntityTypeByEntityIdLocalizationTranslationsByLanguageCode(
          { path: { entityType, entityId, languageCode } },
        )
      if (!result.response?.ok) {
        throw toApiRequestError(result.error, result.response)
      }
    },
    onSuccess: async () => {
      await refresh()
      toast.success(t('library.localization.translationRemoved'))
    },
    onError: () => toast.error(t('library.localization.saveError')),
  })

  if (localizationQuery.isLoading) {
    return <Skeleton className="h-72 w-full" />
  }

  if (localizationQuery.isError || !localizationQuery.data) {
    return (
      <div className="border-destructive/40 bg-destructive/5 border p-4 text-sm">
        <p className="font-medium">{t('library.localization.errorTitle')}</p>
        <Button
          variant="outline"
          size="sm"
          className="mt-3"
          onClick={() => void localizationQuery.refetch()}
        >
          {t('actions.retry')}
        </Button>
      </div>
    )
  }

  const data = localizationQuery.data
  const targetLanguage: SupportedLanguage =
    originalLanguage === 'vi' ? 'en' : 'vi'
  const targetDraft = drafts[targetLanguage]
  const savedTarget = data.translations?.find(
    (translation) => translation.languageCode === targetLanguage,
  )
  const busy =
    originalLanguageMutation.isPending ||
    saveTranslationMutation.isPending ||
    deleteTranslationMutation.isPending

  return (
    <section className="border-border space-y-5 border-t pt-6">
      <div className="flex items-start gap-3">
        <span className="bg-muted flex size-9 shrink-0 items-center justify-center rounded-[6px]">
          <Languages className="size-4" aria-hidden="true" />
        </span>
        <div>
          <h3 className="font-semibold">{t('library.localization.title')}</h3>
          <p className="text-muted-foreground mt-1 text-sm leading-5">
            {t('library.localization.description')}
          </p>
        </div>
      </div>

      <div className="bg-muted/35 border-border grid gap-4 border p-4 sm:grid-cols-[1fr_10rem] sm:items-end">
        <div>
          <p className="text-xs font-semibold tracking-[0.08em] uppercase">
            {t('library.localization.originalData')}
          </p>
          <p className="mt-2 font-medium">{data.originalName}</p>
          {data.originalDescription ? (
            <p className="text-muted-foreground mt-1 text-sm">
              {data.originalDescription}
            </p>
          ) : null}
        </div>
        <div className="space-y-2">
          <Label htmlFor="original-language">
            {t('library.localization.originalLanguage')}
          </Label>
          <Select
            value={originalLanguage}
            disabled={busy}
            onValueChange={(value: SupportedLanguage) => {
              setOriginalLanguage(value)
              originalLanguageMutation.mutate(value)
            }}
          >
            <SelectTrigger id="original-language" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="vi">{t('languages.vi')}</SelectItem>
              <SelectItem value="en">{t('languages.en')}</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="border-border space-y-4 border p-4">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div>
            <p className="font-medium">
              {t('library.localization.translationFor', {
                language: t(`languages.${targetLanguage}`),
              })}
            </p>
            <p className="text-muted-foreground mt-1 text-xs">
              {t('library.localization.translationHelp')}
            </p>
          </div>
          <Badge
            variant={savedTarget?.status === 'Approved' ? 'default' : 'outline'}
          >
            {savedTarget
              ? t(
                  `library.localization.status.${savedTarget.status?.toLowerCase()}`,
                )
              : t('library.localization.notTranslated')}
          </Badge>
        </div>

        <div className="space-y-2">
          <Label htmlFor={`translation-name-${targetLanguage}`}>
            {t('library.localization.translationName')}
          </Label>
          <Input
            id={`translation-name-${targetLanguage}`}
            value={targetDraft.name}
            maxLength={250}
            onChange={(event) =>
              updateDraft(setDrafts, targetLanguage, 'name', event.target.value)
            }
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`translation-description-${targetLanguage}`}>
            {t('library.localization.translationDescription')}
          </Label>
          <Textarea
            id={`translation-description-${targetLanguage}`}
            value={targetDraft.description}
            maxLength={500}
            onChange={(event) =>
              updateDraft(
                setDrafts,
                targetLanguage,
                'description',
                event.target.value,
              )
            }
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`translation-status-${targetLanguage}`}>
            {t('library.localization.translationStatus')}
          </Label>
          <Select
            value={targetDraft.status}
            onValueChange={(value: 'Draft' | 'Approved') =>
              updateDraft(setDrafts, targetLanguage, 'status', value)
            }
          >
            <SelectTrigger
              id={`translation-status-${targetLanguage}`}
              className="w-full"
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Draft">
                {t('library.localization.status.draft')}
              </SelectItem>
              <SelectItem value="Approved">
                {t('library.localization.status.approved')}
              </SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-wrap justify-end gap-2">
          {savedTarget ? (
            <Button
              variant="ghost"
              disabled={busy}
              onClick={() => deleteTranslationMutation.mutate(targetLanguage)}
            >
              <Trash2 aria-hidden="true" />
              {t('library.localization.removeTranslation')}
            </Button>
          ) : null}
          <Button
            disabled={busy || !targetDraft.name.trim()}
            onClick={() => saveTranslationMutation.mutate(targetLanguage)}
          >
            {saveTranslationMutation.isPending ? (
              <LoaderCircle className="animate-spin" aria-hidden="true" />
            ) : null}
            {t('library.localization.saveTranslation')}
          </Button>
        </div>
      </div>

      {data.isFallback ? (
        <p className="text-muted-foreground text-xs leading-5">
          {t('library.localization.fallbackNotice', {
            language: t(
              `languages.${data.resolvedLanguageCode === 'en' ? 'en' : 'vi'}`,
            ),
          })}
        </p>
      ) : null}
    </section>
  )
}

function toDraft(
  data: BusinessDataTranslationBundleResDto,
  languageCode: SupportedLanguage,
): TranslationDraft {
  const translation = data.translations?.find(
    (item) => item.languageCode === languageCode,
  )
  return translation
    ? {
        name: translation.name ?? '',
        description: translation.description ?? '',
        status: translation.status === 'Approved' ? 'Approved' : 'Draft',
      }
    : { ...emptyDraft }
}

function updateDraft<K extends keyof TranslationDraft>(
  setDrafts: Dispatch<
    SetStateAction<Record<SupportedLanguage, TranslationDraft>>
  >,
  languageCode: SupportedLanguage,
  key: K,
  value: TranslationDraft[K],
) {
  setDrafts((current) => ({
    ...current,
    [languageCode]: { ...current[languageCode], [key]: value },
  }))
}
