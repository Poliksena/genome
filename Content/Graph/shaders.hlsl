
#if 0
$ubershader DRAW POINT|LINE
#endif

#define BLOCK_SIZE 256
#define WARP_SIZE 16
#define HALF_BLOCK BLOCK_SIZE/2

struct PARTICLE3D {
	float3	Position; // 3 coordinates
	float3	Velocity;
	float4	Color0;
	float	Size0;
	float	TotalLifeTime;
	float	LifeTime;
	int		LinksPtr;
	int		LinksCount;
	float3	Acceleration;
	float	Mass;
	float	Charge;
	int		Id;
	float	ColorType;
	int		Count;
	int		Group;
	int		Information;
	float	Energy;
	float3	Force;
	int		Cluster;
};

//struct 
struct LinkId {
	int id;
};


struct PARAMS {
	float4x4	View;
	float4x4	Projection;
	int			MaxParticles;
	float		DeltaTime;
	float		LinkSize;
	float		CalculationRadius;
	float		Mass;
	int			StartIndex;
	int			EndIndex;
};


struct Link {
	int		par1;
	int		par2;
	float	length;
	float	force2;
	float3	orientation;
	float	weight;	
	int		linkType;
	float4	Color;
	float	Width;
	float	Time;
	float	TotalLifeTime;
	float	LifeTime;
};



SamplerState						Sampler				: 	register(s0);
Texture2D							Texture 			: 	register(t0);
Texture2D							Stroke 				: 	register(t4);
Texture2D							Border 				: 	register(t5);

RWStructuredBuffer<PARTICLE3D>		particleBufferSrc	: 	register(u0);
StructuredBuffer<PARTICLE3D>		GSResourceBuffer	:	register(t1);

StructuredBuffer<LinkId>			linksPtrBuffer		:	register(t2);
StructuredBuffer<Link>				linksBuffer			:	register(t3);

StructuredBuffer<PARTICLE3D>		particleReadBuffer	:	register(t4);
StructuredBuffer<PARTICLE3D>		particleReadBuffer2	:	register(t5);

RWStructuredBuffer<float4>			energyRWBuffer		:	register(u1);

cbuffer CB1 : register(c0) { 
	PARAMS Params; 
};

/*-----------------------------------------------------------------------------
	Simulation :
-----------------------------------------------------------------------------*/


#ifdef DRAW

struct VSOutput {
int vertexID : TEXCOORD0;
};

struct GSOutput {
	float4	Position : SV_Position;
	float2	TexCoord : TEXCOORD0;
	float4	Color    : COLOR0;
	float3	Normal	 : TEXCOORD1;
};


VSOutput VSMain( uint vertexID : SV_VertexID )
{
	VSOutput output;
	output.vertexID = vertexID;
	return output;
}


#ifdef POINT
[maxvertexcount(12)]
void GSMain( point VSOutput inputPoint[1], inout TriangleStream<GSOutput> outputStream )
{
	GSOutput p0, p1, p2, p3;
	PARTICLE3D prt = GSResourceBuffer[ inputPoint[0].vertexID ];

	float sz				=  prt.Size0;		
	float time				= prt.LifeTime;
	float4 pos				=	float4( prt.Position.xyz, 1 );
	float4 posV				=	mul( pos, Params.View );
		
	float texRight	= prt.ColorType;
	float texLeft	= texRight - 0.1f;

	p0.Position = mul( posV + float4( -sz/2, -sz*sqrt(3)/6, -sz*sqrt(3)/6, 0 ) , Params.Projection );
	p0.TexCoord = float2(0, 0);
	p0.Color = prt.Color0;


	p1.Position = mul( posV + float4( 0, sz*sqrt(3)/3, -sz*sqrt(3)/6, 0 ) , Params.Projection );
	p1.TexCoord = float2(1, 0);
	p1.Color = prt.Color0;


	p2.Position = mul( posV + float4(  sz/2, -sz*sqrt(3)/6, -sz*sqrt(3)/6, 0 ) , Params.Projection );
	p2.TexCoord = float2(1, 1);
	p2.Color = prt.Color0;


	p3.Position = mul( posV + float4(  0, 0, sz*sqrt(3)/3, 0 ) , Params.Projection );
	p3.TexCoord = float2(0, 1);
	p3.Color = prt.Color0;

	float4x4	World = float4x4(1, 0, 0, pos.x,
								 0, 1, 0, pos.y,
								 0, 0, 1, pos.z,
								 0, 0, 0, pos.w);
	
	float3 normal = -cross(p0.Position - p1.Position, p2.Position - p1.Position);
	normal = mul(normal, World);
	normal = normalize(normal);
	p0.Normal = normal;
	p1.Normal = normal;
	p2.Normal = normal;

	outputStream.Append(p0);
	outputStream.Append(p1);
	outputStream.Append(p2);
	outputStream.RestartStrip( );

	normal = -cross(p0.Position - p2.Position, p3.Position - p2.Position);
	normal = mul(normal, World);
	normal = normalize(normal);
	p0.Normal = normal;
	p2.Normal = normal;
	p3.Normal = normal;
	
	outputStream.Append(p0);
	outputStream.Append(p2);
	outputStream.Append(p3);
	outputStream.RestartStrip();

	normal = -cross(p0.Position - p3.Position, p1.Position - p3.Position);
	normal = mul(normal, World);
	normal = normalize(normal);
	p0.Normal = normal;
	p1.Normal = normal;
	p3.Normal = normal;
	
	outputStream.Append(p0);
	outputStream.Append(p3);
	outputStream.Append(p1);
	outputStream.RestartStrip( );

	normal = cross(p1.Position - p3.Position, p2.Position - p3.Position);
	normal = mul(normal, World);
	normal = normalize(normal);
	p1.Normal = normal;
	p2.Normal = normal;
	p3.Normal = normal;
	
	outputStream.Append(p1);
	outputStream.Append(p3);
	outputStream.Append(p2);
	outputStream.RestartStrip( );
}
#endif

#ifdef LINE
[maxvertexcount(24)]
void GSMain( point VSOutput inputLine[1], inout TriangleStream<GSOutput> outputStream )
{
	Link lk = linksBuffer[ inputLine[0].vertexID ];

	PARTICLE3D end1 = GSResourceBuffer[ lk.par1 ];
	PARTICLE3D end2 = GSResourceBuffer[ lk.par2 ];

	float4 pos1 = float4( end1.Position.xyz, 1 );
	float4 pos2 = float4( end2.Position.xyz, 1 );

	GSOutput p1, p2, p3, p4;

	pos1 = mul(pos1 , Params.View);
	pos2 = mul(pos2 , Params.View);
	float3 dir = normalize(pos2 - pos1);
	if (length(dir) == 0 ) return;

	float3 side = normalize(cross(dir, float3(0,0,-1)));					
	
	p1.TexCoord		=	float2(0, 1);
	p2.TexCoord		=	float2(0, 0);
	p3.TexCoord		=	float2(0, 1);
	p4.TexCoord		=	float2(0, 0);
									
	p1.Color		=	lk.Color;
	p2.Color		=	lk.Color;
	p3.Color		=	lk.Color;
	p4.Color		=	lk.Color;
				
	p1.Normal		=	float3(0, 0, 0);
	p2.Normal		=	float3(0, 0, 0);
	p3.Normal		=	float3(0, 0, 0);
	p4.Normal		=	float3(0, 0, 0);
				
	p1.Position = mul( pos1 + float4(side * lk.Width, 0)  /*+ float4(dir * end1.Size0 , 0)*/, Params.Projection ) ;	
	p2.Position = mul( pos1 - float4(side * lk.Width, 0)  /*+ float4(dir * end1.Size0 , 0)*/, Params.Projection ) ;	
	p3.Position = mul( pos2 + float4(side * lk.Width, 0)  /*- float4(dir * end2.Size0 , 0)*/, Params.Projection ) ;	
	p4.Position = mul( pos2 - float4(side * lk.Width, 0)  /*- float4(dir * end2.Size0 , 0)*/, Params.Projection ) ;	
	
	outputStream.Append(p1);
	outputStream.Append(p2);
	outputStream.Append(p3);
	outputStream.Append(p4);
	outputStream.RestartStrip();			
}
#endif

#ifdef LINE
float4 PSMain( GSOutput input ) : SV_Target
{
	return input.Color;
}
#endif

#ifdef POINT
float4 PSMain( GSOutput input ) : SV_Target
{
	float3 amb = float3(100,149,237) / 256.0f / 4;
	float3 sun = float3(255,240,120) / 256.0f * 1;
	float3 light = (0.2 + 0.8*dot(input.Normal, normalize(float3(2,3,1)))) * sun + amb;
	return float4(light * input.Color, 1);
	//return input.Color;

}
#endif


#endif