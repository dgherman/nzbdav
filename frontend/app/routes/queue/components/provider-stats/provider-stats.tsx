import { Card, OverlayTrigger, Tooltip } from 'react-bootstrap';
import type { ProviderStatsResponse } from '~/clients/backend-client.server';
import styles from './provider-stats.module.css';

const operationDescriptions: { [key: string]: string } = {
    'BODY': 'Downloaded article content only (the actual file data, most common)',
    'ARTICLE': 'Downloaded article with headers (less efficient, rarely used)',
    'STAT': 'Checked if article exists (no download, just verification)',
    'HEAD': 'Fetched article metadata only (size, date, etc.)',
    'DATE': 'Got server time (system operation, not download-related)'
};

export function ProviderStats({ stats }: { stats: ProviderStatsResponse | null }) {
    if (!stats || stats.totalOperations === 0) {
        return null;
    }

    const formatNumber = (num: number) => {
        return num.toLocaleString();
    };

    const getTimeAgo = (timestamp: string) => {
        const now = new Date();
        const then = new Date(timestamp);
        const diffMs = now.getTime() - then.getTime();
        const diffMins = Math.floor(diffMs / 60000);

        if (diffMins < 1) return 'just now';
        if (diffMins === 1) return '1 minute ago';
        if (diffMins < 60) return `${diffMins} minutes ago`;

        const diffHours = Math.floor(diffMins / 60);
        if (diffHours === 1) return '1 hour ago';
        return `${diffHours} hours ago`;
    };

    return (
        <Card className={styles.statsCard}>
            <Card.Body>
                <div className={styles.header}>
                    <h5 className={styles.title}>Provider Usage (Last 24 Hours)</h5>
                    <span className={styles.updated}>
                        Updated {getTimeAgo(stats.calculatedAt)}
                    </span>
                </div>

                {stats.providers.length === 0 ? (
                    <p className={styles.noData}>No provider usage data available</p>
                ) : (
                    <div className={styles.providersGrid}>
                        {stats.providers.map((provider) => (
                            <div key={provider.providerHost} className={styles.providerCard}>
                                <div className={styles.providerHeader}>
                                    <span className={styles.providerHost}>{provider.providerHost}</span>
                                    <span className={styles.providerBadge}>
                                        {provider.providerType}
                                    </span>
                                </div>
                                <div className={styles.providerStats}>
                                    <div className={styles.totalOps}>
                                        <span className={styles.opsCount}>
                                            {formatNumber(provider.totalOperations)}
                                        </span>
                                        <span className={styles.opsLabel}>operations</span>
                                        <span className={styles.percentage}>
                                            ({provider.percentageOfTotal.toFixed(1)}%)
                                        </span>
                                    </div>
                                    <div className={styles.operationBreakdown}>
                                        {Object.entries(provider.operationCounts).map(([opType, count]) => (
                                            <OverlayTrigger
                                                key={opType}
                                                placement="top"
                                                overlay={
                                                    <Tooltip id={`tooltip-${provider.providerHost}-${opType}`}>
                                                        {operationDescriptions[opType] || opType}
                                                    </Tooltip>
                                                }
                                            >
                                                <div className={styles.opType}>
                                                    <span className={styles.opTypeName}>{opType}:</span>
                                                    <span className={styles.opTypeCount}>{formatNumber(count)}</span>
                                                </div>
                                            </OverlayTrigger>
                                        ))}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </Card.Body>
        </Card>
    );
}
