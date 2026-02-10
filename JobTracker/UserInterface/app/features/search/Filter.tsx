import { Select } from '@mantine/core'
import { useEffect, useState } from 'react'
import { Tag } from '../../types/tag/tag'
import { sendPhotinoRequest } from '../../utils/photino'
import { TagsCombobox } from './TagsCombobox'

interface FilterProps {
  onFilterChange: (filters: {
    source: string,
    date: Date | null,
    location: string,
    tags: number[]
  }) => void
}

type Filters = {
  source: string,
  date: Date | null,
  location: string,
  tags: Tag[]
}

export default function Filter({ onFilterChange }: FilterProps) {
  const [filters, setFilters] = useState<Filters>({
    source: '',
    date: null as unknown as Date,
    location: '',
    tags: [],
  })

  const [tags, setTags] = useState<Tag[]>([])

  useEffect(() => {
    fetchTags()
  }, [])

  const fetchTags = async () => {
    try {
      const response = await sendPhotinoRequest<Tag[]>(
        'tags.getTags',
        { test: 'anything' }
      )
      console.log("fetched");
      setTags(response)
    } catch (err) {
      console.error('Failed to fetch tags:', err)
    }
  }

  const handleStringFilterChange = (
    key: Exclude<keyof Filters, 'tags'>,
    value: string
  ) => {
    const newFilters = { ...filters, [key]: value }
    setFilters(newFilters)

    onFilterChange({
      ...newFilters,
      tags: newFilters.tags.map((t: Tag) => t.Id),
    })
  }

  const handleTagsChange = (value: Tag[]) => {
    const newFilters = { ...filters, tags: value }
    setFilters(newFilters)

    onFilterChange({
      ...newFilters,
      tags: value.map(t => t.Id)
    })
  }

  const handleDateFilterChange = (value: string) => {
    // Convert string to Date | null
    const convertDateFilter = (value: string): Date | null => {
      const now = new Date();
      switch (value) {
        case 'today':
          return new Date(now.getFullYear(), now.getMonth(), now.getDate());
        case 'week':
          const weekAgo = new Date(now);
          weekAgo.setDate(now.getDate() - 7);
          return weekAgo;
        case 'month':
          const monthAgo = new Date(now);
          monthAgo.setMonth(now.getMonth() - 1);
          return monthAgo;
        default:
          return null;
      }
    };

    const newFilters = { ...filters, date: convertDateFilter(value) };
    setFilters(newFilters);

    onFilterChange({
      ...newFilters,
      tags: newFilters.tags.map((t: Tag) => t.Id),
    });
  };


  return (
    <div className="border-neutral-700">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Select
          label="Sources"
          placeholder="All Sources"
          data={[
            { value: '', label: 'All Sources' },
            { value: 'LinkedIn', label: 'LinkedIn' },
            { value: 'Arbetsförmedlingen', label: 'Arbetsförmedlingen' },
            { value: 'Indeed', label: 'Indeed' },
          ]}
          value={filters.source}
          onChange={(value) =>
            handleStringFilterChange('source', value || '')
          }
        />

        <Select
          label="Date posted"
          placeholder="Any time"
          data={[
            { value: '', label: 'Any Time' },
            { value: 'today', label: 'Today' },
            { value: 'week', label: 'Past Week' },
            { value: 'month', label: 'Past Month' },
          ]}
          value={filters.date ? 'custom' : ""}
          onChange={(value) => handleDateFilterChange(value || '')}
        />

        <Select
          label="Location"
          placeholder="All Locations"
          data={[
            { value: '', label: 'All Locations' },
            { value: 'Stockholm', label: 'Stockholm' },
            { value: 'Gothenburg', label: 'Gothenburg' },
            { value: 'Malmö', label: 'Malmö' },
            { value: 'Remote', label: 'Remote' },
          ]}
          value={filters.location}
          onChange={(value) =>
            handleStringFilterChange('location', value || '')
          }
        />

        <TagsCombobox
          label="Tags"
          placeholder="None"
          tags={tags}
          value={filters.tags}
          onChange={handleTagsChange}
        />
      </div>
    </div>
  )
}