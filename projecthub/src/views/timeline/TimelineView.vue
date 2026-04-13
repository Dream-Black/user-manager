<template>
  <div class="timeline-page">
    <div class="page-header">
      <div class="header-content">
        <h1 class="page-title">活动时间线</h1>
        <p class="page-subtitle">追踪团队的重要事件和里程碑</p>
      </div>
      <div class="header-actions">
        <t-button theme="primary">
          <template #icon><AddIcon /></template>
          添加事件
        </t-button>
      </div>
    </div>

    <!-- 年份筛选 -->
    <div class="year-filter">
      <t-radio-group v-model="selectedYear" variant="default-filled">
        <t-radio-button v-for="year in years" :key="year" :value="year">{{ year }}年</t-radio-button>
      </t-radio-group>
    </div>

    <!-- 时间线 -->
    <div class="timeline">
      <div
        v-for="(month, mIndex) in timelineData"
        :key="month.name"
        class="timeline-month"
        :style="{ animationDelay: `${mIndex * 0.1}s` }"
      >
        <div class="month-marker">
          <div class="marker-dot" :style="{ background: month.color }"></div>
          <div class="marker-line"></div>
        </div>

        <div class="month-content">
          <div class="month-header">
            <h3 class="month-name">{{ month.name }}</h3>
            <span class="month-count">{{ month.events.length }} 个事件</span>
          </div>

          <div class="events-list">
            <div
              v-for="(event, eIndex) in month.events"
              :key="event.id"
              class="event-card"
              :style="{ animationDelay: `${(mIndex * 2 + eIndex) * 0.05}s` }"
            >
              <div class="event-date">{{ event.date }}</div>
              <div class="event-body">
                <div class="event-icon" :style="{ background: event.color }">
                  <component :is="event.icon" />
                </div>
                <div class="event-info">
                  <h4 class="event-title">{{ event.title }}</h4>
                  <p class="event-description">{{ event.description }}</p>
                  <div class="event-meta">
                    <div class="event-author">
                      <div class="author-avatar" :style="{ background: event.authorColor }">
                        {{ event.author.charAt(0) }}
                      </div>
                      <span>{{ event.author }}</span>
                    </div>
                    <div class="event-tags">
                      <t-tag v-for="tag in event.tags" :key="tag" size="small" variant="outline">
                        {{ tag }}
                      </t-tag>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { markRaw } from 'vue'

const selectedYear = ref(2026)
const years = [2024, 2025, 2026]

const timelineData = ref([
  {
    name: '2026年4月', color: '#2196F3',
    events: [
      {
        id: 1, date: '04-13', title: 'Website Redesign v2.0 上线',
        description: '新版官网正式发布，包含全新的UI设计和更好的用户体验',
        icon: markRaw(RocketIcon), color: '#2196F3',
        author: '张三', authorColor: '#2196F3',
        tags: ['里程碑', '发布']
      },
      {
        id: 2, date: '04-10', title: '团队扩展 - 新增3名成员',
        description: '前端团队新增2名工程师，后端新增1名架构师',
        icon: markRaw(UserAddIcon), color: '#4CAF50',
        author: '李四', authorColor: '#4CAF50',
        tags: ['团队', '招聘']
      },
      {
        id: 3, date: '04-05', title: 'App Development 项目启动',
        description: '移动端APP开发项目正式启动，计划6月底完成',
        icon: markRaw(FlagIcon), color: '#FF9800',
        author: '王五', authorColor: '#FF9800',
        tags: ['项目', '启动']
      }
    ]
  },
  {
    name: '2026年3月', color: '#9C27B0',
    events: [
      {
        id: 4, date: '03-28', title: 'Q1季度复盘会议',
        description: '总结Q1工作成果，制定Q2目标计划',
        icon: markRaw(ChartIcon), color: '#9C27B0',
        author: '赵六', authorColor: '#9C27B0',
        tags: ['复盘', '会议']
      },
      {
        id: 5, date: '03-15', title: 'API Integration v2.0 完成',
        description: '第三方API集成服务升级完成，性能提升30%',
        icon: markRaw(CheckCircleIcon), color: '#10B981',
        author: '孙七', authorColor: '#10B981',
        tags: ['完成', 'API']
      }
    ]
  },
  {
    name: '2026年2月', color: '#00BCD4',
    events: [
      {
        id: 6, date: '02-14', title: '情人节特别活动上线',
        description: '配合市场部门上线的情人节主题活动',
        icon: markRaw(HeartIcon), color: '#E91E63',
        author: '周八', authorColor: '#E91E63',
        tags: ['活动', '营销']
      }
    ]
  }
])

import { AddIcon, RocketIcon, UserAddIcon, FlagIcon, ChartIcon, CheckCircleIcon, HeartIcon } from 'tdesign-icons-vue-next'
</script>

<style scoped>
.timeline-page { max-width: 1000px; margin: 0 auto; }
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; animation: fadeInUp 0.5s ease; }
.header-content { display: flex; flex-direction: column; gap: 4px; }
.page-title { font-size: 28px; font-weight: 700; color: var(--text-primary); margin: 0; }
.page-subtitle { font-size: 14px; color: var(--text-secondary); margin: 0; }

.year-filter { margin-bottom: 32px; animation: fadeInUp 0.5s ease 0.1s backwards; }

.timeline { position: relative; padding-left: 40px; }
.timeline::before { content: ''; position: absolute; left: 15px; top: 0; bottom: 0; width: 2px; background: var(--border-color); }

.timeline-month { position: relative; padding-bottom: 40px; animation: fadeInUp 0.6s ease backwards; }

.month-marker { position: absolute; left: -40px; display: flex; flex-direction: column; align-items: center; }
.marker-dot { width: 16px; height: 16px; border-radius: var(--radius-full); border: 3px solid var(--bg-page); box-shadow: 0 0 0 2px var(--border-color); }
.marker-line { flex: 1; width: 2px; background: var(--border-color); margin-top: 8px; }

.month-content { background: var(--bg-container); border-radius: var(--radius-xl); padding: 24px; border: 1px solid var(--border-color); }

.month-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; padding-bottom: 16px; border-bottom: 1px solid var(--border-light); }
.month-name { font-size: 18px; font-weight: 700; color: var(--text-primary); margin: 0; }
.month-count { font-size: 13px; color: var(--text-tertiary); }

.events-list { display: flex; flex-direction: column; gap: 16px; }

.event-card { display: flex; gap: 16px; padding: 16px; background: var(--bg-page); border-radius: var(--radius-lg); transition: all var(--transition-fast); animation: fadeInUp 0.4s ease backwards; }
.event-card:hover { transform: translateX(4px); box-shadow: var(--shadow-sm); }

.event-date { font-size: 14px; font-weight: 700; color: var(--text-secondary); min-width: 48px; }

.event-body { display: flex; gap: 16px; flex: 1; }
.event-icon { width: 44px; height: 44px; border-radius: var(--radius-lg); display: flex; align-items: center; justify-content: center; color: white; flex-shrink: 0; }
.event-icon svg { width: 22px; height: 22px; }

.event-info { flex: 1; }
.event-title { font-size: 15px; font-weight: 600; color: var(--text-primary); margin: 0 0 6px 0; }
.event-description { font-size: 13px; color: var(--text-secondary); margin: 0 0 12px 0; line-height: 1.5; }

.event-meta { display: flex; justify-content: space-between; align-items: center; }
.event-author { display: flex; align-items: center; gap: 8px; font-size: 12px; color: var(--text-tertiary); }
.author-avatar { width: 24px; height: 24px; border-radius: var(--radius-full); display: flex; align-items: center; justify-content: center; color: white; font-size: 10px; font-weight: 600; }
.event-tags { display: flex; gap: 6px; }
</style>
