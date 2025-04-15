const express = require('express');
const router = express.Router();

// Get all vulnerabilities
router.get('/', (req, res) => {
  // This would normally fetch from a database
  // For now, we'll return mock data
  const mockData = [
    {
      id: 1,
      name: 'CVE-2023-1234',
      severity: 'High',
      affectedSystems: ['Windows Server 2019', 'Windows 10'],
      description: 'Remote code execution vulnerability in Windows',
      remediation: 'Apply Microsoft Security Update KB5025224',
      dateDiscovered: '2023-06-15'
    },
    {
      id: 2,
      name: 'CVE-2023-5678',
      severity: 'Critical',
      affectedSystems: ['Apache 2.4.x'],
      description: 'Buffer overflow vulnerability in Apache HTTP Server',
      remediation: 'Upgrade to Apache 2.4.57 or later',
      dateDiscovered: '2023-07-20'
    }
  ];
  
  res.json(mockData);
});

// Get vulnerability by ID
router.get('/:id', (req, res) => {
  // In production, this would look up in a database
  const vulnId = parseInt(req.params.id);
  
  if (vulnId === 1) {
    return res.json({
      id: 1,
      name: 'CVE-2023-1234',
      severity: 'High',
      affectedSystems: ['Windows Server 2019', 'Windows 10'],
      description: 'Remote code execution vulnerability in Windows',
      remediation: 'Apply Microsoft Security Update KB5025224',
      dateDiscovered: '2023-06-15',
      details: 'This vulnerability allows remote attackers to execute arbitrary code via a crafted network message to an affected Windows system.',
      references: [
        'https://msrc.microsoft.com/update-guide/vulnerability/CVE-2023-1234',
        'https://nvd.nist.gov/vuln/detail/CVE-2023-1234'
      ],
      cvssScore: 8.7
    });
  } else if (vulnId === 2) {
    return res.json({
      id: 2,
      name: 'CVE-2023-5678',
      severity: 'Critical',
      affectedSystems: ['Apache 2.4.x'],
      description: 'Buffer overflow vulnerability in Apache HTTP Server',
      remediation: 'Upgrade to Apache 2.4.57 or later',
      dateDiscovered: '2023-07-20',
      details: 'A buffer overflow vulnerability in Apache HTTP Server allows attackers to execute arbitrary code by sending specially crafted requests.',
      references: [
        'https://httpd.apache.org/security/vulnerabilities_24.html',
        'https://nvd.nist.gov/vuln/detail/CVE-2023-5678'
      ],
      cvssScore: 9.8
    });
  } else {
    return res.status(404).json({ error: 'Vulnerability not found' });
  }
});

module.exports = router;